// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using LibreLancer.Client;
using LibreLancer.Data;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.MBases;
using LibreLancer.Data.Schema.Save;
using LibreLancer.Data.Schema.Solar;
using LibreLancer.Data.Schema.Ships;
using LibreLancer.Missions;
using LibreLancer.Net;
using LibreLancer.Net.Protocol;
using LibreLancer.Net.Protocol.RpcPackets;
using LibreLancer.Server.Components;
using LibreLancer.Server.RandomMissions;
using LibreLancer.World;
using LiteNetLib;
using DisconnectReason = LibreLancer.Net.DisconnectReason;
using Ship = LibreLancer.Data.GameData.Ship;
using StarSystem = LibreLancer.Data.GameData.World.StarSystem;
using SystemObject = LibreLancer.Data.GameData.World.SystemObject;

namespace LibreLancer.Server
{
    public class Player : IServerPlayer
    {
        public const float DefaultVisitDistance = 10000f;

        // ID
        public int ID = 0;

        private static int _gid = 0;

        // Reference
        public IPacketClient Client;
        public GameServer Game;
        public SpacePlayer? Space;
        public BasesidePlayer? Baseside;

        private MissionRuntime? msnRuntime;
        private PreloadObject[] msnPreload = null!;
        private readonly DynamicThn thns = new();
        private bool jumpPending;

        private ConcurrentQueue<Action> saveActions = new();

        // State
        public NetCharacter? Character;

        public MPlayer MPlayer = null!;
        public DateTime StartTime;
        public string Name = "Player";
        public string SaveFolder = null!;
        public string System = null!;
        public string? Base;
        private string? locationRoom;
        public Vector3 Position;
        public Quaternion Orientation;
        public NetObjective Objective;
        public Vector3? ActiveRandomMissionPosition;

        public StoryProgress Story = null!;

        // Store so we can choose the correct character from the index
        public List<SelectableCharacter> CharacterList = null!;

        // Respawn?
        public bool Dead = false;

        private Guid playerGuid; //:)
        public Guid AccountId => playerGuid;
        public NetResponseHandler ResponseHandler;

        private RemoteClientPlayer rpcClient;

        public RemoteClientPlayer RpcClient => rpcClient;

        public Player(IPacketClient client, GameServer game, Guid playerGuid)
        {
            this.Client = client;
            this.Game = game;
            this.playerGuid = playerGuid;
            ID = Interlocked.Increment(ref _gid);
            ResponseHandler = new NetResponseHandler();
            rpcClient = new RemoteClientPlayer(client, ResponseHandler);
        }

        public void SetObjective(NetObjective objective, bool history)
        {
            FLLog.Info("Server", $"Set player objective to {objective.Kind}: {objective.Ids}");
            Objective = objective;
            rpcClient.SetObjective(objective, history);
        }

        public void SetManeuversLocked(bool locked)
        {
            rpcClient.SetManeuverLock(locked);
        }

        public void SetManeuverEnabled(ManeuverType maneuver, bool enabled)
        {
            rpcClient.SetManeuverEnabled(maneuver, enabled);
        }

        public void UpdateMissionRuntime(double elapsed)
        {
            msnRuntime?.Update(elapsed);

            if (Space != null)
            {
                while (worldActions.Count > 0)
                    worldActions.Dequeue()();
            }
        }

        private void HandleBaseEntry(string baseName)
        {
            if (MissionRuntime != null)
            {
                FLLog.Info("Mission", $"Mission runtime handling base enter: {Story?.CurrentMission?.Nickname}");
                MissionRuntime.SpaceExit();
                MissionRuntime.BaseEnter(baseName);
                MissionRuntime.CheckMissionScript();
            }
            else
            {
                FLLog.Debug("Mission", "No mission runtime available during base enter");
            }
        }

        private void HandleSpaceEntry()
        {
            if (MissionRuntime != null)
            {
                MissionRuntime.PlayerLaunch();
                MissionRuntime.CheckMissionScript();
                MissionRuntime.EnteredSpace();
            }
        }

        public void ShipKilledByPlayer(Ship ship)
        {
            Character!.IncrementShipKillCount(ship); // SP only
            using var nc = Character.BeginTransaction();

            switch (ship.ShipType)
            {
                case ShipType.Fighter:
                    nc.UpdateFightersKilled(Character.Statistics.FightersKilled + 1);
                    break;
                case ShipType.Freighter:
                    nc.UpdateFreightersKilled(Character.Statistics.FreightersKilled + 1);
                    break;
                case ShipType.Capital:
                    nc.UpdateBattleshipsKilled(Character.Statistics.BattleshipsKilled + 1);
                    break;
                case ShipType.Transport:
                    nc.UpdateTransportsKilled(Character.Statistics.TransportsKilled + 1);
                    break;
            }

            rpcClient.UpdateStatistics(Character.Statistics);
        }

        public bool InTradelane;

        public void StartTradelane(GameObject ring, Quaternion orientation)
        {
            rpcClient.StartTradelane(ring, orientation);
            InTradelane = true;
        }

        public void TradelaneRing(GameObject ring)
        {
            if (InTradelane)
                rpcClient.TradelaneRing(ring);
        }

        public void TradelaneDisrupted()
        {
            rpcClient.TradelaneDisrupted();
            InTradelane = false;
        }

        public void EndTradelane()
        {
            rpcClient.EndTradelane();
            InTradelane = false;
        }

        public void MissionSuccess()
        {
            if (Story?.CurrentMission == null)
            {
                msnRuntime = null;
                ActiveRandomMissionPosition = null;
                SetObjective(new NetObjective(), false);
                return;
            }

            loadTriggers = [];
            Story.Advance(this);
        }

        public void ResetMissionDockingRestrictions()
        {
            if (MPlayer == null)
                return;

            MPlayer.CanDock = 1;
            MPlayer.CanTl = 1;
            MPlayer.DockExceptions.Clear();
            MPlayer.TlExceptions.Clear();
            AllowedDockUpdate();
        }

        public void SPMissionFailure(int ids)
        {
            rpcClient.StoryMissionFailed(ids);
        }

        public MissionRuntime? MissionRuntime => msnRuntime;

        public void StartRandomMission(GeneratedRandomMission mission, NetMissionOffer netOffer)
        {
            ActiveRandomMissionPosition = mission.Parameters.TargetPosition;
            msnRuntime = new MissionRuntime(mission.CreateScript(), this, []);
            rpcClient.SetActiveRandomMission(netOffer);
            // Keep the objective in the mission runtime as well as on the client. This
            // makes the accepted offer a real active mission and lets the normal space
            // gameplay objective handler build the best-path route on launch.
            msnRuntime.SetCurrentObjective(GeneratedRandomMission.TargetObjectiveNickname, false);
        }

        public bool AllowFreetimePopulation =>
            Story?.CurrentStory == null ||
            Story.CurrentMission == null ||
            Story.CurrentStory.CashUp > 0 ||
            Story.CurrentStory.Nickname.EndsWith("_loaded", StringComparison.OrdinalIgnoreCase);

        public void AddRTC(string rtc)
        {
            lock (thns)
            {
                thns.AddRTC(rtc);
                rpcClient.UpdateThns(thns.Pack());
            }
        }

        public void RemoveRTC(string rtc)
        {
            lock (thns)
            {
                thns.RemoveRTC(rtc);
                rpcClient.UpdateThns(thns.Pack());
            }
        }

        public void AddAmbient(string script, string room, string _base)
        {
            lock (thns)
            {
                thns.AddAmbient(script, room, _base);
                rpcClient.UpdateThns(thns.Pack());
            }
        }

        public void RemoveAmbient(string script)
        {
            lock (thns)
            {
                thns.RemoveAmbient(script);
                rpcClient.UpdateThns(thns.Pack());
            }
        }

        void IServerPlayer.RTCComplete(string rtc)
        {
            lock (thns)
            {
                thns.RemoveRTC(rtc);
                rpcClient.UpdateThns(thns.Pack());
                msnRuntime?.FinishRTC(rtc);
            }
        }

        void IServerPlayer.StoryNPCSelect(string name, string room, string _base)
        {
            msnRuntime?.StoryNPCSelect(name, room, _base);
        }

        void IServerPlayer.BaseNpcInteract(string _base, string room, string npc, int optionId)
        {
            HandleBaseNpcInteraction(_base, room, npc, optionId);
        }

        void IServerPlayer.BaseNpcAccept(string _base, string room, string npc, int optionId)
        {
            HandleBaseNpcInteraction(_base, room, npc, optionId, false);
        }

        void IServerPlayer.AcceptMissionOffer(int seed)
        {
            Baseside?.AcceptMissionOffer(seed);
        }

        void IServerPlayer.ClosedPopup(string id)
        {
            msnRuntime?.ClosePopup(id);
        }

        void IServerPlayer.LineSpoken(uint hash)
        {
            msnRuntime?.LineFinished(hash);
        }

        void IServerPlayer.OnLocationEnter(string _base, string room)
        {
            locationRoom = room;
            msnRuntime?.EnterLocation(room, _base);
        }

        void IServerPlayer.OnLocationExit(string _base, string room)
        {
            if (locationRoom?.Equals(room, StringComparison.OrdinalIgnoreCase) == true)
                locationRoom = null;
            msnRuntime?.ExitLocation(room, _base);
        }

        private void HandleBaseNpcInteraction(string _base, string room, string npcName, int optionId,
            bool showDialog = true)
        {
            FLLog.Info("NPC", $"NPC interaction request: base={_base}, room={room}, npc={npcName}, option={optionId}, currentBase={Base}, currentRoom={locationRoom}");

            if (Base != null && !string.Equals(Base, _base, StringComparison.OrdinalIgnoreCase))
            {
                FLLog.Warning("NPC", $"Ignoring NPC interaction outside current base: requested={_base}, current={Base}");
                return;
            }

            var baseName = Base ?? _base;
            var baseData = Game.GameData.Items.Bases.Get(baseName) ?? Game.GameData.Items.Bases.Get(_base);
            if (baseData == null)
            {
                FLLog.Warning("NPC", $"NPC interaction base data not found: {_base}");
                return;
            }

            var roomData = baseData.Rooms.Get(room) ?? baseData.Rooms.Get(locationRoom);
            var npc = roomData?.Npcs.FirstOrDefault(x =>
                x.Nickname.Equals(npcName, StringComparison.OrdinalIgnoreCase));

            if (npc == null)
            {
                roomData = baseData.Rooms.FirstOrDefault(x => x.Npcs.Any(n =>
                    n.Nickname.Equals(npcName, StringComparison.OrdinalIgnoreCase)));
                npc = roomData?.Npcs.FirstOrDefault(x =>
                    x.Nickname.Equals(npcName, StringComparison.OrdinalIgnoreCase));
            }

            if (npc == null)
            {
                FLLog.Warning("NPC", $"NPC not found in base data: base={_base}, requestedRoom={room}, currentRoom={locationRoom}, npc={npcName}");
                rpcClient.ShowBaseNpcDialog(new NetBaseNpcDialog { Npc = npcName });
                return;
            }

            var interactionRoom = roomData!.Nickname;
            if (optionId == 0 || !showDialog)
                IncrementNpcTalk(npc, baseName, interactionRoom);
            var contents = ResolveNpcOption(
                npc,
                optionId,
                out var focusSystemHash,
                out var focusObjectHash);
            if (optionId == 0)
                contents = GetNextKnowledge(npc)?.Ids2 ?? GetNextRumor(npc);
            if (!showDialog)
            {
                if (focusSystemHash != 0 && focusObjectHash != 0)
                {
                    FLLog.Info("NPC", $"Knowledge accepted: npc={npc.Nickname}, system={focusSystemHash}, object={focusObjectHash}");
                    rpcClient.ShowBaseNpcDialog(new NetBaseNpcDialog
                    {
                        Npc = npc.Nickname,
                        FocusSystemHash = focusSystemHash,
                        FocusObjectHash = focusObjectHash
                    });
                }
                else if (BaseNpcOptionId.Type(optionId) == BaseNpcOptionType.Knowledge)
                {
                    var index = BaseNpcOptionId.Index(optionId);
                    FLLog.Warning("NPC", $"Knowledge accepted without map focus: npc={npc.Nickname}, option={optionId}, objects={string.Join(",", npc.Know.ElementAtOrDefault(index)?.Objects ?? [])}");
                }
                return;
            }

            var dialog = BuildNpcDialog(
                npc,
                _base,
                interactionRoom,
                contents,
                focusSystemHash,
                focusObjectHash);
            FLLog.Info("NPC", $"Sending NPC dialog: npc={npc.Nickname}, room={interactionRoom}, contents={dialog.Contents}, options={dialog.Options.Length}");
            rpcClient.ShowBaseNpcDialog(dialog);
        }

        private NetBaseNpcDialog BuildNpcDialog(
            BaseNpc npc,
            string _base,
            string room,
            int contents,
            uint focusSystemHash = 0,
            uint focusObjectHash = 0)
        {
            var options = new List<NetBaseNpcOption>();
            var reputation = Character?.Reputation.GetReputation(npc.Affiliation) ?? 0;
            var rumorThreshold = BaseNpcRules.RumorReputationThreshold(npc.Affiliation);

            if (contents != 0)
            {
                var knowledgeIndex = npc.Know.FindIndex(x => x.Ids2 == contents);
                if (knowledgeIndex >= 0)
                {
                    var know = npc.Know[knowledgeIndex];
                    if (know.Ids1 != 0 && know.Ids2 != 0 &&
                        !HasRumor(know.Ids2) && reputation >= know.RepThreshold)
                    {
                        options.Add(new NetBaseNpcOption
                        {
                            Id = BaseNpcOptionId.Encode(
                                knowledgeIndex,
                                BaseNpcOptionType.Knowledge),
                            Kind = BaseNpcOptionKind.Knowledge,
                            Text = know.Ids1,
                            Contents = know.Ids2,
                            Price = know.Price,
                            ObjectNames = GetKnowledgeObjectNames(know)
                        });
                    }
                }
                else
                {
                    var rumorIndex = npc.Rumors.FindIndex(x => x.Ids == contents);
                    if (rumorIndex >= 0)
                    {
                        options.Add(new NetBaseNpcOption
                        {
                            Id = BaseNpcOptionId.Encode(
                                rumorIndex,
                                BaseNpcOptionType.Rumor),
                            Kind = BaseNpcOptionKind.Rumor,
                            Text = contents,
                            Contents = contents
                        });
                    }
                }
            }

            if (options.Count == 0)
            {
                for (var i = 0; i < npc.Rumors.Count; i++)
                {
                    var rumor = npc.Rumors[i];
                    if (HasRumor(rumor.Ids) || !RumorAvailable(rumor) || reputation < rumorThreshold)
                        continue;

                    options.Add(new NetBaseNpcOption
                    {
                        Id = BaseNpcOptionId.Encode(i, BaseNpcOptionType.Rumor),
                        Kind = BaseNpcOptionKind.Rumor,
                        Text = rumor.Ids,
                        Contents = rumor.Ids
                    });
                }

                for (var i = 0; i < npc.Bribes.Count; i++)
                {
                    var bribe = npc.Bribes[i];
                    if (Character == null || !BaseNpcRules.IsBribeAvailable(bribe, Character.Reputation))
                        continue;

                    options.Add(NetBaseNpcOption.ForBribe(i, bribe));
                }

                for (var i = 0; i < npc.Know.Count; i++)
                {
                    var know = npc.Know[i];
                    if (know.Ids1 == 0 || know.Ids2 == 0 ||
                        HasRumor(know.Ids2) || reputation < know.RepThreshold)
                        continue;

                    options.Add(new NetBaseNpcOption
                    {
                        Id = BaseNpcOptionId.Encode(i, BaseNpcOptionType.Knowledge),
                        Kind = BaseNpcOptionKind.Knowledge,
                        Text = know.Ids1,
                        Contents = know.Ids2,
                        Price = know.Price,
                        ObjectNames = GetKnowledgeObjectNames(know)
                    });
                }

                if (npc.Mission != null && Baseside?.NetMissionOffers.Length > 0)
                {
                    options.Add(new NetBaseNpcOption
                    {
                        Id = 0,
                        Kind = BaseNpcOptionKind.Mission,
                        Text = 1350
                    });
                }
            }

            return new NetBaseNpcDialog
            {
                Npc = npc.Nickname,
                IndividualName = npc.IndividualName,
                Contents = options.Any(x => x.Kind == BaseNpcOptionKind.Knowledge) ? 0 : contents,
                Options = options.ToArray(),
                FocusSystemHash = focusSystemHash,
                FocusObjectHash = focusObjectHash
            };
        }

        private int GetNextRumor(BaseNpc npc)
        {
            var reputation = Character?.Reputation.GetReputation(npc.Affiliation) ?? 0;
            var rumorThreshold = BaseNpcRules.RumorReputationThreshold(npc.Affiliation);
            var rumors = npc.Rumors
                .Where(rumor => rumor.Ids != 0 &&
                                !HasRumor(rumor.Ids) &&
                                RumorAvailable(rumor) &&
                                reputation >= rumorThreshold)
                .ToArray();
            return rumors.Length == 0 ? 0 : rumors[Random.Shared.Next(rumors.Length)].Ids;
        }

        private NpcKnow? GetNextKnowledge(BaseNpc npc)
        {
            var reputation = Character?.Reputation.GetReputation(npc.Affiliation) ?? 0;
            return npc.Know.FirstOrDefault(know =>
                know.Ids1 != 0 &&
                know.Ids2 != 0 &&
                !HasRumor(know.Ids2) &&
                reputation >= know.RepThreshold);
        }

        private string[] GetKnowledgeObjectNames(NpcKnow know)
        {
            var names = new List<string>();
            foreach (var objectNickname in know.Objects)
            {
                var systemObject = Game.GameData.Items.Systems
                    .SelectMany(system => system.Objects)
                    .FirstOrDefault(obj => obj.Nickname.Equals(
                        objectNickname,
                        StringComparison.OrdinalIgnoreCase));
                var objectName = systemObject == null
                    ? ""
                    : Game.GameData.GetString(systemObject.IdsName);
                names.Add(string.IsNullOrWhiteSpace(objectName)
                    ? objectNickname
                    : objectName);
            }

            return names.ToArray();
        }

        private int ResolveNpcOption(
            BaseNpc npc,
            int optionId,
            out uint focusSystemHash,
            out uint focusObjectHash)
        {
            focusSystemHash = 0;
            focusObjectHash = 0;

            var type = BaseNpcOptionId.Type(optionId);
            var index = BaseNpcOptionId.Index(optionId);

            if (type == BaseNpcOptionType.Rumor)
            {
                if (index >= 0 && index < npc.Rumors.Count)
                {
                    var rumor = npc.Rumors[index];
                    var reputation = Character?.Reputation.GetReputation(npc.Affiliation) ?? 0;
                    if (!HasRumor(rumor.Ids) && RumorAvailable(rumor) &&
                        reputation >= BaseNpcRules.RumorReputationThreshold(npc.Affiliation))
                    {
                        MPlayer.Rumors.Add(new SaveRumor(new HashValue(rumor.Ids), 1));
                        return rumor.Ids;
                    }
                }
            }
            else if (type == BaseNpcOptionType.Bribe)
            {
                if (index >= 0 && index < npc.Bribes.Count)
                {
                    var bribe = npc.Bribes[index];
                    if (Character != null &&
                        BaseNpcRules.IsBribeAvailable(bribe, Character.Reputation) &&
                        Character.Credits >= bribe.Price)
                    {
                        using var transaction = Character.BeginTransaction();
                        transaction.UpdateCredits(Character.Credits - bribe.Price);
                        transaction.UpdateReputation(bribe.Faction!, BaseNpcRules.BribeReputation);
                        UpdateCurrentInventory();
                        UpdateCurrentReputations();
                        return bribe.Ids;
                    }
                }
            }
            else if (type == BaseNpcOptionType.Knowledge)
            {
                if (index >= 0 && index < npc.Know.Count)
                {
                    var know = npc.Know[index];
                    var reputation = Character?.Reputation.GetReputation(npc.Affiliation) ?? 0;
                    if (HasRumor(know.Ids2))
                    {
                        (focusSystemHash, focusObjectHash) = RevealKnownObjects(know.Objects);
                        FLLog.Info("NPC", $"Knowledge already known; restoring map focus: ids={know.Ids2}");
                        return know.Ids2;
                    }

                    if (reputation >= know.RepThreshold &&
                        Character != null && Character.Credits >= know.Price)
                    {
                        using (var transaction = Character.BeginTransaction())
                        {
                            transaction.UpdateCredits(Character.Credits - know.Price);
                            MPlayer.Rumors.Add(new SaveRumor(new HashValue(know.Ids2), 1));
                        }

                        UpdateCurrentInventory();
                        (focusSystemHash, focusObjectHash) = RevealKnownObjects(know.Objects);
                        return know.Ids2;
                    }

                    FLLog.Warning("NPC", $"Knowledge purchase rejected: npc={npc.Nickname}, ids={know.Ids2}, credits={Character?.Credits ?? 0}, price={know.Price}, reputation={reputation}, threshold={know.RepThreshold}");
                }
            }

            return 0;
        }

        private (uint SystemHash, uint ObjectHash) RevealKnownObjects(IEnumerable<string> objectNames)
        {
            uint focusSystemHash = 0;
            uint focusObjectHash = 0;

            foreach (var objectName in objectNames)
            {
                StarSystem? targetSystem = null;
                SystemObject? targetObject = null;
                foreach (var system in Game.GameData.Items.Systems)
                {
                    targetObject = system.Objects.FirstOrDefault(obj =>
                        obj.Nickname.Equals(objectName, StringComparison.OrdinalIgnoreCase));
                    if (targetObject == null)
                        continue;

                    targetSystem = system;
                    break;
                }

                if (targetSystem == null || targetObject == null)
                {
                    FLLog.Warning("NPC", $"Known NPC location not found: {objectName}");
                    continue;
                }

                if (targetObject.Archetype == null)
                {
                    FLLog.Warning("NPC", $"Known NPC location has no archetype: {objectName}");
                    continue;
                }

                if (!targetObject.Archetype.CanVisit)
                {
                    FLLog.Warning("NPC", $"Known NPC location is not visitable: {objectName}, type={targetObject.Archetype.Type}");
                    continue;
                }

                var objectHash = FLHash.CreateID(targetObject.Nickname);
                VisitObject(targetSystem, targetObject, objectHash);
                if (focusObjectHash == 0)
                {
                    focusSystemHash = targetSystem.CRC;
                    focusObjectHash = objectHash;
                    FLLog.Info("NPC", $"Known NPC map focus resolved: object={targetObject.Nickname}, system={targetSystem.Nickname}, systemHash={focusSystemHash}, objectHash={focusObjectHash}");
                }
            }

            return (focusSystemHash, focusObjectHash);
        }

        private bool RumorAvailable(BaseNpcRumor rumor)
        {
            var mission = Story?.MissionNum ?? 0;
            if (rumor.Start != null && mission < rumor.Start.Index)
                return false;
            if (rumor.End != null && mission > rumor.End.Index)
                return false;
            return true;
        }

        private bool HasRumor(int ids)
        {
            var hash = new HashValue(ids);
            return MPlayer.Rumors.Any(x => x.Item == hash);
        }

        private void IncrementNpcTalk(BaseNpc npc, string _base, string room)
        {
            var npcHash = new HashValue(npc.Nickname);
            var locationHash = new HashValue(FLHash.CreateLocationID(_base, room));
            var index = MPlayer.VNPCs.FindIndex(x => x.ItemA == npcHash && x.ItemB == locationHash);
            if (index < 0)
            {
                MPlayer.VNPCs.Add(new VNPC(npcHash, locationHash, 1, 0));
                return;
            }

            var old = MPlayer.VNPCs[index];
            MPlayer.VNPCs[index] = old with { Unknown1 = old.Unknown1 + 1 };
        }

        public ulong GetShipWorth()
        {
            if (Character!.Ship == null)
                return 0;
            return (ulong) (Game.GameData.Items.GetShipPrice(Character.Ship) * TradeConstants.SHIP_RESALE_MULTIPLIER);
        }

        public long CalculateNetWorth()
        {
            var worth = Character!.Credits + (long) GetShipWorth();

            foreach (var item in Character.Items)
            {
                if (item.Equipment!.Good == null)
                {
                    continue;
                }

                long unitPrice = item.Equipment.Good.Ini.Price;
                if (item.Equipment is not CommodityEquipment)
                    unitPrice = (long) (unitPrice * TradeConstants.EQUIP_RESALE_MULTIPLIER);
                worth += unitPrice * item.Count;
            }

            return worth;
        }

        private void BeginGame(NetCharacter c, SaveGame? sg)
        {
            Character = c;
            MPlayer = sg?.MPlayer ?? new() { CanDock = 1, CanTl = 1 };
            StartTime = DateTime.UtcNow;
            Name = Character.Name!;
            rpcClient.UpdatePlayTime(c.Time, StartTime);
            rpcClient.UpdateBaselinePrices(Game.BaselineGoodPrices);
            UpdateCurrentReputations();
            UpdateCurrentInventory();
            rpcClient.UpdateStatistics(c.Statistics);

            if (SinglePlayer)
            {
                rpcClient.UpdateVisits(new VisitBundle() { Visits = c.GetAllVisitFlags() });
            }
            else
            {
                rpcClient.UpdateVisits(VisitBundle.Compress(c.GetAllVisitFlags()));
            }

            Base = Character.Base;
            System = Character.System!;
            Position = Character.Position;
            Orientation = Character.Orientation;

            if (Orientation == Quaternion.Zero)
            {
                Orientation = Quaternion.Identity;
            }

            foreach (var player in Game.AllPlayers.Where(x => x != this))
            {
                player.RpcClient.OnPlayerJoin(ID, Name!);
            }

            rpcClient.ListPlayers(Character.Admin);

            if (sg != null)
            {
                InitStory(sg);
            }

            rpcClient.UpdateCharacterProgress((int) Character.Rank, (long) (Story?.NextLevelWorth ?? -1));
            AllowedDockUpdate();

            if (Base != null)
            {
                PlayerEnterBase();
            }
            else
            {
                SpaceInitialSpawn(null);
            }

            Game.ServerEvents.Enqueue(new ServerEvent
            {
                Type = ServerEventType.CharacterConnected,
                TimeUtc = DateTime.UtcNow,
                Payload = new CharacterConnectedEventPayload(this)
            });
        }

        public void OpenSaveGame(SaveGame sg)
        {
            if (File.Exists(Path.Combine(SaveFolder, "AutoSave.fl")))
            {
                rpcClient.SPSetAutosave(Path.Combine(SaveFolder, "AutoSave.fl"));
            }

            BeginGame(NetCharacter.OpenSaveGame(Game, sg), sg);
        }

        public void AddCash(long credits)
        {
            if (Character == null) return;

            using var c = Character.BeginTransaction();

            c.UpdateCredits(Character.Credits + credits);

        }

        private void SpaceInitialSpawn(SaveGame? sg)
        {
            ClearScan();
            var sys = Game.GameData.Items.Systems.Get(System);
            Game.Worlds.RequestWorld(sys!, (world) =>
            {
                Space = new SpacePlayer(world, this);
                world.EnqueueAction(() =>
                {
                    rpcClient.SpawnPlayer(ID, System, world.GameWorld.CrcTranslation.ToArray(), Objective, Position,
                        Orientation, Character!.GetDestroyedParts(), world.CurrentTick);
                    var pship = world.SpawnPlayer(this, Position, Orientation);
                    world.Population.PopulateInitialAroundPlayer(pship);

                    // Ensure mission runtime is properly initialized when spawning in space
                    HandleSpaceEntry();
                });
            }, msnPreload);
        }

        private IEnumerable<NetSoldShip> GetSoldShips()
        {
            var b = Game.GameData.Items.Bases.Get(Base)!;

            foreach (var s in b.SoldShips)
            {
                ulong goodsPrice = 0;

                foreach (var eq in s.Package.Addons)
                {
                    goodsPrice += (ulong) ((long) b.GetUnitPrice(eq.Equipment) * eq.Amount);
                }

                yield return new NetSoldShip()
                {
                    ShipCRC = (int)s.Package.Ship.CRC,
                    PackageCRC = (int) FLHash.CreateID(s.Package.Nickname),
                    HullPrice = (ulong) s.Package.BasePrice,
                    PackagePrice = (ulong) s.Package.BasePrice + goodsPrice,
                    Rank = s.Rank
                };
            }
        }

        private void PlayerEnterBase()
        {
            // load base
            Space = null;
            Baseside = new BasesidePlayer(this, Game.GameData.Items.Bases.Get(Base)!);
            // fetch news articles
            var news = new List<NewsArticle>();

            foreach (var x in Game.GameData.Items.News.QueryNews(
                         Baseside.BaseData!, Story?.MissionNum ?? (Game.GameData.Items.Ini.Storyline.Items.Count - 1)))
            {
                news.Add(new NewsArticle()
                {
                    Icon = x.Icon!, Headline = x.Headline,
                    Logo = x.Logo!, Text = x.Text
                });
            }

            // update
            using (var c = Character!.BeginTransaction())
            {
                c.UpdatePosition(Base, System, Position, Orientation);
                c.VisitBase(Baseside.BaseData!.CRC);
            }

            HandleBaseEntry(Base!);

            // send to player
            lock (thns)
            {
                rpcClient.UpdateStatistics(Character.Statistics);
                rpcClient.BaseEnter(Base!, Objective, thns.Pack(), news.ToArray(), Baseside.BaseData.SoldGoods
                    .Select(x => new SoldGood()
                    {
                        GoodCRC = FLHash.CreateID(x.Good.Ini.Nickname),
                        Price = x.Price,
                        Rank = x.Rank,
                        Rep = x.Rep,
                        ForSale = x.ForSale
                    }).ToArray(), GetSoldShips().ToArray(), Baseside.NetMissionOffers,
                    Character.GetDestroyedParts());
            }
        }

        private uint[] loadTriggers = null!;

        public void LoadMission()
        {
            if (Story?.CurrentMission != null)
            {
                FLLog.Info("Mission",
                    $"Loading mission: {Story.CurrentMission.Nickname} with {loadTriggers?.Length ?? 0} saved triggers");

                // Load the mission script
                var missionIni = Game.GameData.Items.Ini.LoadMissionIni(Story.CurrentMission);
                msnRuntime = new MissionRuntime(new(missionIni!, Game.GameData.Items), this, loadTriggers!);
                msnPreload = msnRuntime.Script.CalculatePreloads(Game.GameData);
                // rpcClient.SetPreloads(msnPreload); // TODO: Re-implement

                // Ensure mission runtime is properly initialized
                msnRuntime.Update(0.0);

                // Debug: Log the mission script details
                FLLog.Debug("Mission",
                    $"Mission script loaded: {missionIni!.Ships.Count} ships, {missionIni.Solars.Count} solars, {missionIni.NPCs.Count} NPCs");

                // If we're in space, trigger mission events to restore state
                if (Space != null)
                {
                    FLLog.Info("Mission",
                        $"Initializing mission runtime in space for mission: {Story.CurrentMission.Nickname}");

                    HandleSpaceEntry();

                    // Give the mission runtime a chance to process initial triggers
                    msnRuntime.Update(0.1);
                }
                else
                {
                    FLLog.Debug("Mission", "Mission loaded but not in space - will restore when entering space");
                }
            }
            else
            {
                FLLog.Debug("Mission", "No mission to load - CurrentMission is null");
            }
        }

        public void UpdateProgress()
        {
            rpcClient.UpdateCharacterProgress((int) Character!.Rank, (long) (Story?.NextLevelWorth ?? -1));
        }

        private void InitStory(SaveGame sg)
        {
            var msn = sg.StoryInfo?.Mission ?? "No_Mission";
            var missionNum = sg.StoryInfo?.MissionNum ?? 0;

            Story = new StoryProgress();
            var storyline = Game.GameData.Items.Ini.Storyline;

            missionNum = Math.Clamp(missionNum, 0, storyline.Items.Count - 1);

            if (Game.GameData.Items.Ini.ContentDll.AlwaysMission13)
            {
                missionNum = 41;
                msn = "Mission_13";
            }

            if (!msn.Equals("No_Mission", StringComparison.OrdinalIgnoreCase))
            {
                Story.CurrentMission = storyline.Missions.FirstOrDefault(x =>
                    x.Nickname.Equals(msn, StringComparison.OrdinalIgnoreCase));
            }

            if (missionNum < storyline.Items.Count)
            {
                Story.CurrentStory = storyline.Items[missionNum];
            }

            Story.MissionNum = missionNum;
            Story.NextLevelWorth = (sg.StoryInfo?.DeltaWorth ?? -1);

            lock (thns)
            {
                thns.Reset();

                if (sg.MissionState != null)
                {
                    foreach (var rtc in sg.MissionState.Rtcs)
                        thns.AddRTC(rtc.Script!);

                    foreach (var amb in sg.MissionState.Ambients)
                    {
                        var _base = Game.GameData.Items.Bases.Get(amb.Base.Hash);
                        var room = _base!.Rooms.Get(amb.Room.Hash);
                        thns.AddAmbient(amb.Script!, room!.Nickname, _base.Nickname);
                    }
                }
            }

            FLLog.Debug("Story", $"{Story.CurrentStory.Nickname}, {Story.MissionNum}");

            loadTriggers = sg.TriggerSave.Select(x => (uint) x.Trigger).ToArray();

            // Only load mission if we have a valid mission
            if (Story?.CurrentMission != null)
            {
                LoadMission();
            }
            else
            {
                FLLog.Debug("Mission",
                    $"Not loading mission: CurrentMission={Story?.CurrentMission?.Nickname}, Base={Base}");
            }
        }

        private Queue<Action> worldActions = new();

        public void MissionWorldAction(Action a)
        {
            worldActions.Enqueue(a);
        }

        public async Task OnLoggedIn()
        {
            try
            {
                FLLog.Info("Server", "Account logged in");
                CharacterList = (await Game.Database.PlayerLogin(playerGuid))!;

                if (CharacterList == null)
                {
                    FLLog.Info("Server", $"Account {playerGuid} is banned, kicking.");
                    Client.Disconnect(DisconnectReason.Banned);

                    Game.ServerEvents.Enqueue(new ServerEvent
                    {
                        Type = ServerEventType.PlayerDisconnected,
                        TimeUtc = DateTime.UtcNow,
                        Payload = new PlayerDisconnectedEventPayload(this, DisconnectReason.Banned)
                    });

                    return;
                }

                Client.SendPacket(new LoginSuccessPacket(), PacketDeliveryMethod.ReliableOrdered);
                Client.SendPacket(new OpenCharacterListPacket()
                {
                    Info = new CharacterSelectInfo()
                    {
                        ServerName = Game.ServerName,
                        ServerDescription = Game.ServerDescription,
                        ServerNews = Game.ServerNews,
                        Characters = CharacterList,
                    }
                }, PacketDeliveryMethod.ReliableOrdered);
                packetQueueTask = Task.Factory.StartNew(ProcessPacketQueue, TaskCreationOptions.LongRunning);

                Game.ServerEvents.Enqueue(new ServerEvent
                {
                    Type = ServerEventType.PlayerConnected,
                    TimeUtc = DateTime.UtcNow,
                    Payload = new PlayerConnectedEventPayload(this)
                });
            }
            catch (Exception? ex)
            {
                // TODO: is this possible to trigger
                while (ex != null)
                {
                    FLLog.Error("Player", ex.Message);
                    FLLog.Error("Player", ex.StackTrace!);
                    ex = ex.InnerException!;
                }

                Client.Disconnect(DisconnectReason.LoginError);

                Game.ServerEvents.Enqueue(new ServerEvent
                {
                    Type = ServerEventType.PlayerDisconnected,
                    TimeUtc = DateTime.UtcNow,
                    Payload = new PlayerDisconnectedEventPayload(this, DisconnectReason.LoginError)
                });
            }
        }

        public bool SinglePlayer => Client is LocalPacketClient;

        public void SendSPUpdate(SPUpdatePacket update) =>
            Client.SendPacket(update, PacketDeliveryMethod.SequenceA);

        public void SendMPUpdate(PackedUpdatePacket update) =>
            Client.SendPacket(update, PacketDeliveryMethod.SequenceA);

        private BufferBlock<IPacket> inputPackets = new();
        private Task packetQueueTask = null!;

        public void EnqueuePacket(IPacket packet)
        {
            inputPackets.Post(packet);
        }

        // Long running task, quits when we finish consuming the collection
        private async Task ProcessPacketQueue()
        {
            while (await inputPackets.OutputAvailableAsync())
            {
                var pkt = await inputPackets.ReceiveAsync();

                try
                {
                    await ProcessPacketDirect(pkt).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    FLLog.Error("Player",
                        $"Exception thrown while processing packets. Force disconnect {Character?.Name ?? "null"}");
                    FLLog.Error("Exception", ex.ToString());
                    Client.Disconnect(DisconnectReason.ConnectionError);
                    Disconnected();
                    break;
                }
            }

            FLLog.Debug("Player", "ProcessPacketQueue() finished");
        }

        public async Task ProcessPacketDirect(IPacket packet)
        {
            if (ResponseHandler.HandlePacket(packet))
                return;
            if (await GeneratedProtocol.HandleIServerPlayer(packet, this, Client))
                return;
            if (Space != null && await GeneratedProtocol.HandleISpacePlayer(packet, Space, Client))
                return;
            if (Baseside != null && await GeneratedProtocol.HandleIBasesidePlayer(packet, Baseside, Client))
                return;

            if (packet is InputUpdatePacket p)
                Space?.World.InputsUpdate(this, p);
            else
            {
                FLLog.Info("Player", $"Disconnecting player, invalid packet type {packet.GetType()}");
                Client.Disconnect(DisconnectReason.ConnectionError);
                Disconnected();
            }
        }

        private NetLoadout? _scanLoadout;
        private ObjNetId _scanId;

        public void ClearScan()
        {
            if (_scanLoadout != null)
            {
                _scanLoadout = null;
                RpcClient.ClearScan();
            }
        }

        public void UpdateScan(ObjNetId id, NetLoadout loadout)
        {
            _scanLoadout ??= new();
            var diff = NetLoadoutDiff.Create(_scanLoadout, loadout);

            if (_scanId != id ||
                diff.ApplyArchetype || diff.ApplyHealth ||
                diff.Items != null)
            {
                RpcClient.UpdateScan(id, diff);
            }

            _scanLoadout = loadout;
            _scanId = id;
        }

        void IServerPlayer.RTCMissionAccepted()
        {
            msnRuntime?.MissionAccepted();
        }

        public void RTCMissionRejected()
        {
            msnRuntime?.MissionRejected();
        }

        public void LevelUp()
        {
            using var c = Character!.BeginTransaction();
            c.UpdateRank(Character.Rank + 1);
            rpcClient.UpdateCharacterProgress((int) Character.Rank, (long) (Story?.NextLevelWorth ?? -1));
        }

        void IServerPlayer.RequestCharacterDB()
        {
            Client.SendPacket(new NewCharacterDBPacket()
            {
                Factions = Game.GameData.Items.Ini.NewCharDB.Factions,
                Packages = Game.GameData.Items.Ini.NewCharDB.Packages,
                Pilots = Game.GameData.Items.Ini.NewCharDB.Pilots
            }, PacketDeliveryMethod.ReliableOrdered);
        }

        async Task<bool> IServerPlayer.SelectCharacter(int index)
        {
            if (index >= 0 && index < CharacterList.Count)
            {
                var sc = CharacterList[index];
                FLLog.Info("Server", $"opening id {sc.Id}");

                if (!Game.CharactersInUse.Add(sc.Id))
                {
                    FLLog.Info("Server", $"Character `{sc.Name}` is already in use");
                    return false;
                }

                BeginGame(await NetCharacter.FromDb(sc.Id, Game), null);
                return true;
            }
            else
            {
                return false;
            }
        }

        Task<bool> IServerPlayer.DeleteCharacter(int index)
        {
            if (index < 0 || index >= CharacterList.Count)
                return Task.FromResult(false);
            var sc = CharacterList[index];
            Game.Database.DeleteCharacter(sc.Id);
            CharacterList.Remove(sc);
            return Task.FromResult(true);
        }

        async Task<bool> IServerPlayer.CreateNewCharacter(string name, int index)
        {
            if (!Game.Database.NameInUse(name))
            {
                FLLog.Info("Player", $"New char: {name}");
                SelectableCharacter? sel = null;
                long id = await Game.Database.AddCharacter(playerGuid,
                    (db) => { NetCharacter.SaveToDbCharacter(Game, Game.NewCharacter(name, index), db); });
                sel = (await NetCharacter.FromDb(id, Game)).ToSelectable();
                CharacterList.Add(sel);
                Client.SendPacket(new AddCharacterPacket()
                {
                    Character = sel
                }, PacketDeliveryMethod.ReliableOrdered);
                return true;
            }
            else
            {
                FLLog.Info("Player", $"Char name in use: {name}");
                return false;
            }
        }

        public void VisitSystem(StarSystem system)
        {
            var needsFlag = (Character!.GetVisitFlags(system.CRC) & VisitFlags.Visited) != VisitFlags.Visited;
            var needsList = Character.IsSystemVisited(system.CRC);

            if (needsFlag || needsList)
            {
                using var ts = Character.BeginTransaction();

                if (needsFlag)
                {
                    ts.UpdateVisitFlags(system.CRC, VisitFlags.Visited);
                    rpcClient.VisitObject(system.CRC, (byte) VisitFlags.Visited);
                }

                if (needsList)
                {
                    ts.VisitSystem(system.CRC);
                    rpcClient.UpdateStatistics(Character.Statistics);
                }
            }
        }

        public static float GetVisitDistance(SystemObject obj) =>
            MathF.Max(DefaultVisitDistance, (obj.Archetype?.SolarRadius ?? 0f) + DefaultVisitDistance);

        public void VisitZone(StarSystem system, Zone zone)
        {
            if ((zone.VisitFlags & VisitFlags.Hidden) == VisitFlags.Hidden || Character == null)
                return;

            var hash = FLHash.CreateID(zone.Nickname);
            var needsFlag = (Character.GetVisitFlags(hash) & VisitFlags.Visited) != VisitFlags.Visited;
            if (!needsFlag)
                return;

            VisitSystem(system);
            using var ts = Character.BeginTransaction();
            ts.UpdateVisitFlags(hash, zone.VisitFlags | VisitFlags.Visited);
            rpcClient.VisitObject(hash, (byte)(zone.VisitFlags | VisitFlags.Visited));
        }

        public void VisitObject(StarSystem system, SystemObject obj, uint hash)
        {
            if ((obj.Visit & VisitFlags.Hidden) ==
                VisitFlags.Hidden)
            {
                return;
            }

            if (!obj.Archetype!.CanVisit)
            {
                return;
            }

            if (Character == null)
            {
                // HACK: Race condition between disconnect and player being
                // removed from server world.
                return;
            }

            var needsFlag = (Character.GetVisitFlags(hash) & VisitFlags.Visited) != VisitFlags.Visited;
            var needsList = obj.Archetype.Type is ArchetypeType.jumphole or ArchetypeType.jump_hole &&
                            !Character.IsJumpholeVisited(hash);

            if (needsFlag || needsList)
            {
                VisitSystem(system);
                using var ts = Character.BeginTransaction();

                if (needsFlag)
                {
                    ts.UpdateVisitFlags(hash, obj.Visit | VisitFlags.Visited);
                    rpcClient.VisitObject(hash, (byte) (obj.Visit | VisitFlags.Visited));
                }

                if (needsList)
                {
                    ts.VisitJumphole(hash);
                    rpcClient.UpdateStatistics(Character.Statistics);
                }
            }
        }

        private void UpdateCurrentReputations()
        {
            rpcClient.UpdateReputations(Character!.Reputation.Reputations.Select(x => new NetReputation()
            {
                FactionHash = x.Key.CRC,
                Reputation = x.Value
            }).ToArray());
        }

        private PlayerInventory lastInventory = new();

        public void UpdateCurrentInventory(bool resetDestroyedParts = false)
        {
            PlayerInventory newInventory = new()
            {
                Credits = Character!.Credits,
                ShipWorth = GetShipWorth(),
                NetWorth = (ulong) CalculateNetWorth(),
                Loadout = Character.EncodeLoadout()
            };

            var diff = PlayerInventoryDiff.Create(lastInventory, newInventory, resetDestroyedParts);
            lastInventory = newInventory;

            if (diff.Header != 0)
            {
                rpcClient.UpdateInventory(diff);
            }

            Story?.Update(this);
        }

        public void ForceLand(string? target)
        {
            Space?.Leave(false);
            Space = null;
            Base = target;
            PlayerEnterBase();
        }

        public void Despawn(int objId, bool explode)
        {
            rpcClient.DespawnObject(objId, explode);
        }

        public void Killed()
        {
            if (Character != null)
            {
                using var characterTransaction = Character.BeginTransaction();
                characterTransaction.ClearDestroyedParts();
            }
            Space?.Leave(true);
            Space = null;
            Dead = true;
            rpcClient.Killed();
            Base = Character!.Base;
            System = Character.System!;
            Position = Character.Position;
            Orientation = Character.Orientation;
        }

        void IServerPlayer.Respawn()
        {
            if (Dead)
            {
                Dead = false;

                if (Base != null)
                {
                    PlayerEnterBase();
                }
                else
                {
                    SpaceInitialSpawn(null);
                }
            }
        }

        public void AllowedDockUpdate()
        {
            if (MPlayer == null)
                rpcClient.UpdateAllowedDocking(new());
            else
            {
                var ad = new AllowedDocking
                {
                    CanDock = MPlayer.CanDock != 0,
                    CanTl = MPlayer.CanTl != 0
                };

                if (!ad.CanDock)
                {
                    ad.DockExceptions = [];
                    foreach (var ex in MPlayer.DockExceptions)
                        ad.DockExceptions.Add(ex.Hash);
                }

                if (!ad.CanTl)
                {
                    ad.TlExceptions = [];
                    foreach (var ex in MPlayer.TlExceptions)
                        ad.TlExceptions.Add(ex.ItemA);
                }

                rpcClient.UpdateAllowedDocking(ad);
            }
        }

        void IServerPlayer.ChatMessage(ChatCategory category, BinaryChatMessage message)
        {
            string msg0 = message.Segments.Count > 0 ? message.Segments[0].Contents : "";

            if (msg0.Length >= 2 && msg0[0] == '/' && char.IsLetter(msg0[1]))
            {
                FLLog.Info("Console", $"({DateTime.Now} {category}) {Name}: {message}");
                ConsoleCommands.ConsoleCommands.Run(this, msg0.Substring(1));
            }
            else
            {
                FLLog.Info("Chat", $"({DateTime.Now} {category}) {Name}: {message}");

                switch (category)
                {
                    case ChatCategory.System:
                        Game.SystemChatMessage(this, message);
                        break;
                    case ChatCategory.Local:
                        Space?.World.LocalChatMessage(this, message);
                        break;
                }
            }
        }

        public void UpdateWeaponGroup(NetWeaponGroup wg)
        {
        }

        public void RunSave()
        {
            while (saveActions.TryDequeue(out var a))
                a();
        }

        private const string SAVE_ALPHABET = "23456789bcdfghjlmnpqrstvwxyz";

        private static string EncodeTime(long number)
        {
            if (number < 0)
                throw new ArgumentException();
            var builder = new StringBuilder();
            var divisor = (long) SAVE_ALPHABET.Length;

            while (number > 0)
            {
                number = Math.DivRem(number, divisor, out var rem);
                builder.Append(SAVE_ALPHABET[(int) rem]);
            }

            return new string(builder.ToString().Reverse().ToArray().AsSpan(
                builder.Length - 4, 4));
        }

        public Task<string> SaveSP(string? description, int ids, bool isAutoSave, DateTime? timeStamp)
        {
            var completionSource = new TaskCompletionSource<string>();
            saveActions.Enqueue(() =>
            {
                if (Character != null)
                {
                    using var c = Character.BeginTransaction();
                    c.UpdatePosition(Base, System, Position, Orientation);
                    var n = DateTime.UtcNow;
                    c.UpdateTime(Character.Time + (n - StartTime).Seconds);
                    StartTime = n;
                }

                SaveGame sg;

                lock (thns)
                {
                    sg = SaveWriter.CreateSave(Character!, description, ids, timeStamp, Game.GameData, thns.Rtcs,
                        thns.Ambients, Story, MPlayer);
                }

                string path;
                MissionRuntime?.WriteActiveTriggers(sg);

                if (isAutoSave)
                {
                    path = Path.Combine(SaveFolder, "AutoSave.fl");
                }
                else
                {
                    var filename = $"Save0{EncodeTime(DateTimeOffset.Now.ToUnixTimeSeconds())}.fl";
                    path = Path.Combine(SaveFolder, filename);
                    int i = 0;

                    while (File.Exists(path))
                    {
                        filename = $"Save0{EncodeTime(DateTimeOffset.Now.ToUnixTimeSeconds())}{i++}.fl";
                        path = Path.Combine(SaveFolder, filename);
                    }
                }

                IniWriter.WriteIniFile(path, sg.ToIni());
                completionSource.SetResult(path);

                if (isAutoSave || ids != 0)
                {
                    // For the "load autosave" functionality
                    rpcClient.SPSetAutosave(path);
                }
            });
            return completionSource.Task;
        }

        private void LoggedOut()
        {
            if (Character != null)
            {
                using var c = Character.BeginTransaction();
                c.UpdatePosition(Base, System, Position, Orientation);
                c.UpdateTime(Character.Time + (DateTime.UtcNow - StartTime).Seconds);
                Space?.Leave(false);
                Space = null;
                foreach (var player in Game.AllPlayers.Where(x => x != this))
                    player.RpcClient.OnPlayerLeave(ID, Name);
                Game.CharactersInUse.Remove(Character.ID);

                Game.ServerEvents.Enqueue(new ServerEvent
                {
                    Type = ServerEventType.CharacterDisconnected,
                    TimeUtc = DateTime.UtcNow,
                    Payload = new CharacterDisconnectedEventPayload(this)
                });

                Character = null;
            }
            else
            {
                Game.ServerEvents.Enqueue(new ServerEvent
                {
                    Type = ServerEventType.PlayerDisconnected,
                    TimeUtc = DateTime.UtcNow,
                    Payload = new PlayerDisconnectedEventPayload(this, DisconnectReason.Unknown)
                });
            }

        }

        public void Disconnected()
        {
            if (packetQueueTask != null)
            {
                inputPackets.Complete();
                packetQueueTask.Wait(1000);

                Game.ServerEvents.Enqueue(new ServerEvent
                {
                    Type = ServerEventType.PlayerDisconnected,
                    TimeUtc = DateTime.UtcNow,
                    Payload = new PlayerDisconnectedEventPayload(this, DisconnectReason.Unknown)
                });
            }

            LoggedOut();
        }

        public void JumpTo(string system, string target, JumperNpc[] jumpers)
        {
            if (jumpPending)
            {
                FLLog.Info("Player", $"Ignoring duplicate jump request to {system} - {target}");
                return;
            }

            jumpPending = true;
            rpcClient.StartJumpTunnel();
            FLLog.Debug("Player", $"Jumping to {system} - {target}");

            if (Space != null)
            {
                msnRuntime?.SystemExit(System, "Player");
                Space.Leave(false);
            }

            Space = null;
            ClearScan();
            var sys = Game.GameData.Items.Systems.Get(system)!
                ;
            Game.Worlds.RequestWorld(sys, (world) =>
            {
                var obj = sys.Objects.FirstOrDefault((o) =>
                    o.Nickname.Equals(target, StringComparison.OrdinalIgnoreCase));

                System = system;
                Base = null;
                Position = Vector3.Zero;
                Orientation = Quaternion.Identity;

                if (obj == null)
                {
                    FLLog.Error("Server", $"Can't find target {target} to spawn player in {system}");
                }
                else
                {
                    Position = obj.Position;
                    Orientation = obj.Rotation;
                    Position = Vector3.Transform(new Vector3(0, 0, 500), Orientation) +
                               obj.Position; // TODO: This is bad
                }

                Baseside = null;
                Base = null;
                world.EnqueueAction(() =>
                {
                    try
                    {
                        Space = new SpacePlayer(world, this);
                        rpcClient.SpawnPlayer(ID, System, world.GameWorld.CrcTranslation.ToArray(), Objective, Position,
                            Orientation, Character!.GetDestroyedParts(), world.CurrentTick);
                        var pship = world.SpawnPlayer(this, Position, Orientation);
                        world.Population.PopulateInitialAroundPlayer(pship);
                        HandleSpaceEntry();
                        msnRuntime?.SystemEnter(system, "Player");
                    }
                    finally
                    {
                        jumpPending = false;
                    }
                });
                world.DelayAction(() => { world.SpawnJumpers(target, jumpers); }, 4);
            }, msnPreload);
        }

        public void LaunchFromBase()
        {
            if (Character?.Ship == null)
            {
                FLLog.Error("Server", $"{Name} cannot launch without a ship");
                return;
            }

            if (Base == null)
            {
                rpcClient.OnConsoleMessage("You are not on a base.");
                return;
            }

            ClearScan();
            var b = Game.GameData.Items.Bases.Get(Base)!;
            var sys = Game.GameData.Items.Systems.Get(b.System);
            Game.Worlds.RequestWorld(sys!, (world) =>
            {
                Space = new SpacePlayer(world, this);
                var launchBase = Base;
                var obj = sys!.Objects.FirstOrDefault((o) =>
                {
                    return (o.Dock is { Kind: DockKinds.Base } &&
                            o.Dock.Target!.Equals(launchBase, StringComparison.OrdinalIgnoreCase));
                });
                System = b.System!;
                Orientation = Quaternion.Identity;
                Position = Vector3.Zero;

                Baseside = null;
                Base = null;
                world.EnqueueAction(() =>
                {
                    GameObject? undockFrom = null;
                    if (obj != null)
                    {
                        undockFrom = world.GameWorld.GetObject(obj.Nickname);
                        Position = obj.Position;
                        Orientation = obj.Rotation;
                    }
                    else if (launchBase != null)
                    {
                        undockFrom = MissionRuntime?.SpawnMissionSolarForBase(launchBase, world);
                        if (undockFrom != null)
                        {
                            Position = undockFrom.WorldTransform.Position;
                            Orientation = undockFrom.LocalTransform.Orientation;
                        }
                    }

                    if (undockFrom == null)
                    {
                        FLLog.Error("Base", "Can't find object in " + sys.Nickname + " docking to " + b.Nickname);
                    }
                    else
                    {
                        Position = Vector3.Transform(new Vector3(0, 0, 500), Orientation) +
                                   Position; // TODO: This is bad
                    }

                    SDockableComponent? sd = null;
                    var undockIndex = 0;

                    if (undockFrom?.TryGetComponent(out sd) ?? false)
                    {
                        if (!sd!.TryReserveUndockIndex(out undockIndex))
                        {
                            FLLog.Warning("Server", $"Could not reserve spawn point for {undockFrom}");
                            return;
                        }

                        if (!sd.TryGetSpawnPoint(undockIndex, out var tr))
                        {
                            sd.ReleaseUndockIndex(undockIndex);
                            FLLog.Warning("Server", $"Could not get spawn point {undockIndex} for {undockFrom}");
                            return;
                        }
                        Position = tr.Position;
                        Orientation = tr.Orientation;
                    }
                    else
                    {
                        undockFrom = null;
                    }

                    rpcClient.SpawnPlayer(ID, System, world.GameWorld.CrcTranslation.ToArray(), Objective, Position,
                        Orientation, Character!.GetDestroyedParts(), world.CurrentTick);
                    var pship = world.SpawnPlayer(this, Position, Orientation);
                    world.Population.PopulateInitialAroundPlayer(pship);

                    if (undockFrom != null)
                    {
                        sd!.UndockShip(pship, world.GameWorld, undockIndex);
                        rpcClient.UndockFrom(undockFrom, undockIndex);

                    }

                    HandleSpaceEntry();
                });
            }, msnPreload);
        }

        void IServerPlayer.Launch() => LaunchFromBase();
    }
}
