// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using LibreLancer.Client;
using LibreLancer.Data.Save;
using LibreLancer.GameData;
using LibreLancer.GameData.World;
using LibreLancer.Ini;
using LibreLancer.Missions;
using LibreLancer.Net;
using LibreLancer.Net.Protocol;
using LibreLancer.Net.Protocol.RpcPackets;
using LibreLancer.Server.Components;
using LibreLancer.World;

namespace LibreLancer.Server
{
    public class Player : IServerPlayer
    {
        //ID
        public int ID = 0;
        private static int _gid = 0;
        //Reference
        public IPacketClient Client;
        public NetHpidReader HpidReader = new NetHpidReader();
        public GameServer Game;
        public SpacePlayer Space;
        public BasesidePlayer Baseside;

        private MissionRuntime msnRuntime;
        private PreloadObject[] msnPreload;
        private readonly DynamicThn thns = new DynamicThn();


        private ConcurrentQueue<Action> saveActions = new ConcurrentQueue<Action>();
        //State
        public NetCharacter Character;
        public string Name = "Player";
        public string System;
        public string Base;
        public Vector3 Position;
        public Quaternion Orientation;
        public NetObjective Objective;
        public StoryProgress Story;
        //Store so we can choose the correct character from the index
        public List<SelectableCharacter> CharacterList;
        //Respawn?
        public bool Dead = false;

        Guid playerGuid; //:)

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

        public void SetObjective(NetObjective objective)
        {
            FLLog.Info("Server", $"Set player objective to {objective.Kind}: {objective.Ids}");
            Objective = objective;
            rpcClient.SetObjective(objective);
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

        public bool InTradelane;
        public void StartTradelane()
        {
            rpcClient.StartTradelane();
            InTradelane = true;
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
            loadTriggers = Array.Empty<uint>();
            Story.Advance(this);
        }

        public MissionRuntime MissionRuntime => msnRuntime;


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
            msnRuntime?.StoryNPCSelect(name,room,_base);
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
            msnRuntime?.EnterLocation(room, _base);
        }

        public ulong GetShipWorth()
        {
            if(Character.Ship == null)
                return 0;
            return (ulong) (Game.GameData.GetShipPrice(Character.Ship) * TradeConstants.SHIP_RESALE_MULTIPLIER);
        }


        void BeginGame(NetCharacter c, SaveGame sg)
        {
            Character = c;
            Name = Character.Name;
            rpcClient.UpdateBaselinePrices(Game.BaselineGoodPrices);
            UpdateCurrentReputations();
            rpcClient.UpdateInventory(Character.Credits, GetShipWorth(), Character.EncodeLoadout());
            Base = Character.Base;
            System = Character.System;
            Position = Character.Position;
            Orientation = Character.Orientation;
            if(Orientation == Quaternion.Zero)
                Orientation = Quaternion.Identity;
            foreach(var player in Game.AllPlayers.Where(x => x != this))
                player.RpcClient.OnPlayerJoin(ID, Name);
            rpcClient.ListPlayers(Character.Admin);
            if (sg != null) InitStory(sg);
            if (Base != null) {
                PlayerEnterBase();
            } else {
                SpaceInitialSpawn(null);
            }
        }

        public void OpenSaveGame(SaveGame sg) => BeginGame(NetCharacter.FromSaveGame(Game, sg), sg);

        public void AddCash(long credits)
        {
            if (Character == null) return;
            using (var c = Character.BeginTransaction())
            {
                c.UpdateCredits(Character.Credits + credits);
            }
        }

        void SpaceInitialSpawn(SaveGame sg)
        {
            var sys = Game.GameData.Systems.Get(System);
            Game.Worlds.RequestWorld(sys, (world) =>
            {
                Space = new SpacePlayer(world, this);
                world.EnqueueAction(() =>
                {
                    rpcClient.SpawnPlayer(ID, System, world.GameWorld.CrcTranslation.ToArray(), Objective, Position, Orientation, world.CurrentTick);
                    world.SpawnPlayer(this, Position, Orientation);
                    msnRuntime?.PlayerLaunch();
                    msnRuntime?.CheckMissionScript();
                    msnRuntime?.EnteredSpace();
                });
            }, msnPreload);
        }
        bool NewsFind(LibreLancer.Data.Missions.NewsItem ni)
        {
            if (ni.Rank[1] != "mission_end")
                return false;
            foreach(var x in ni.Base)
                if (x.Equals(Base, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        IEnumerable<NetSoldShip> GetSoldShips()
        {
            var b = Game.GameData.Bases.Get(Base);
            foreach (var s in b.SoldShips)
            {
                ulong goodsPrice = 0;
                foreach (var eq in s.Package.Addons)
                {
                    goodsPrice += (ulong) ((long)b.GetUnitPrice(eq.Equipment) * eq.Amount);
                }
                yield return new NetSoldShip()
                {
                    ShipCRC = (int) FLHash.CreateID(s.Package.Ship),
                    PackageCRC = (int)FLHash.CreateID(s.Package.Nickname),
                    HullPrice = (ulong) s.Package.BasePrice,
                    PackagePrice = (ulong)s.Package.BasePrice + goodsPrice
                };
            }
        }

        void PlayerEnterBase()
        {
            //fetch news articles
            List<NewsArticle> news = new List<NewsArticle>();
            foreach (var x in Game.GameData.Ini.News.NewsItems.Where(NewsFind))
            {
                news.Add(new NewsArticle()
                {
                    Icon = x.Icon, Category = x.Category, Headline =  x.Headline,
                    Logo = x.Logo, Text = x.Text
                });
            }
            //load base
            Space = null;
            Baseside = new BasesidePlayer(this, Game.GameData.Bases.Get(Base));
            //update
            using (var c = Character.BeginTransaction())
            {
                c.UpdatePosition(Base, System, Position, Orientation);
            }
            MissionRuntime?.SpaceExit();
            MissionRuntime?.BaseEnter(Base);
            MissionRuntime?.CheckMissionScript();
            //send to player
            lock (thns)
            {
                rpcClient.BaseEnter(Base, Objective, thns.Pack(), news.ToArray(), Baseside.BaseData.SoldGoods.Select(x => new SoldGood()
                {
                    GoodCRC = CrcTool.FLModelCrc(x.Good.Ini.Nickname),
                    Price = x.Price,
                    Rank = x.Rank,
                    Rep = x.Rep,
                    ForSale = x.ForSale
                }).ToArray(), GetSoldShips().ToArray());
            }
        }

        private uint[] loadTriggers;

        public void LoadMission()
        {
            if (Story?.CurrentMission != null)
            {
                msnRuntime = new MissionRuntime(Game.GameData.Ini.LoadMissionIni(Story.CurrentMission), this, loadTriggers);
                msnPreload = msnRuntime.Script.CalculatePreloads(Game.GameData);
                rpcClient.SetPreloads(msnPreload);
                msnRuntime.Update(0.0);
            }
        }
        void InitStory(Data.Save.SaveGame sg)
        {
            var msn = sg.StoryInfo?.Mission ?? "No_Mission";
            var missionNum = sg.StoryInfo?.MissionNum ?? 0;
            if (Game.GameData.Ini.ContentDll.AlwaysMission13) {
                missionNum = 41;
                msn = "Mission_13";
            }
            Story = new StoryProgress();
            var storyline = Game.GameData.Ini.Storyline;
            if (!msn.Equals("No_Mission", StringComparison.OrdinalIgnoreCase))
            {
                Story.CurrentMission = storyline.Missions.FirstOrDefault(x =>
                        x.Nickname.Equals(msn, StringComparison.OrdinalIgnoreCase));
            }
            if (missionNum >= 0 && missionNum < storyline.Items.Count)
                Story.CurrentStory = storyline.Items[missionNum];
            Story.MissionNum = missionNum;
            Story.NextLevelWorth = (sg.StoryInfo?.DeltaWorth ?? -1);
            lock (thns)
            {
                thns.Reset();
                if (sg.MissionState != null) {
                    foreach(var rtc in sg.MissionState.Rtcs)
                        thns.AddRTC(rtc.Script);
                    foreach (var amb in sg.MissionState.Ambients)
                    {
                        var _base = Game.GameData.Bases.Get(amb.Base.Hash);
                        var room = _base.Rooms.Get(amb.Room.Hash);
                        thns.AddAmbient(amb.Script, room.Nickname, _base.Nickname);
                    }
                }
            }
            FLLog.Debug("Story", $"{Story.CurrentStory.Nickname}, {Story.MissionNum}");
            loadTriggers = sg.TriggerSave.Select(x => (uint) x.Trigger).ToArray();
            LoadMission();
        }

        private Queue<Action> worldActions = new Queue<Action>();
        public void MissionWorldAction(Action a)
        {
            worldActions.Enqueue(a);
        }

        public void OnLoggedIn()
        {
            try
            {
                FLLog.Info("Server", "Account logged in");
                if (!Game.Database.PlayerLogin(playerGuid, out CharacterList)) {
                    FLLog.Info("Server", $"Account {playerGuid} is banned, kicking.");
                    Client.Disconnect(DisconnectReason.Banned);
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

                packetQueueTask = Task.Run(ProcessPacketQueue);
            }
            catch (Exception ex)
            {
                FLLog.Error("Player",ex.Message);
                FLLog.Error("Player",
                    ex.StackTrace);
                Client.Disconnect(DisconnectReason.LoginError);
            }
        }

        public bool SinglePlayer => Client is LocalPacketClient;

        public NetHpidWriter HpidWriter => (Client as RemotePacketClient)?.Hpids;

        public void SendSPUpdate(SPUpdatePacket update) =>
            Client.SendPacket(update, PacketDeliveryMethod.SequenceA);

        public void SendMPUpdate(PackedUpdatePacket update) =>
            Client.SendPacket(update, PacketDeliveryMethod.SequenceA);

        public void SpawnPlayer(Player p)
        {
            var lO = p.Character.EncodeLoadout();
            lO.Health = p.Character.Ship.Hitpoints;
            var info = new ShipSpawnInfo()
            {
                Name = new ObjectName(p.Name),
                Position = p.Position,
                Orientation = p.Orientation,
                Loadout = lO
            };
            rpcClient.SpawnShip(p.ID, info);
        }

        public void SendSolars(Dictionary<string, GameObject> solars)
        {
            var si = new List<SolarInfo>();
            foreach (var solar in solars)
            {
                var tr = solar.Value.WorldTransform;
                var info = new SolarInfo()
                {
                    ID = solar.Value.NetID,
                    Name = solar.Value.Name,
                    Nickname = solar.Value.Nickname,
                    Archetype = solar.Value.ArchetypeName,
                    Position = tr.Position,
                    Orientation = tr.Orientation
                };
                if (solar.Value.TryGetComponent<SRepComponent>(out var rep)){
                    info.Faction = rep.Faction?.Nickname;
                }
                if (solar.Value.TryGetComponent<SDockableComponent>(out var dock)){
                    info.Dock = dock.Action;
                }
                si.Add(info);
            }
            rpcClient.SpawnSolar(si.ToArray());
        }

        private BlockingCollection<IPacket> inputPackets = new BlockingCollection<IPacket>();
        private Task packetQueueTask;

        public void EnqueuePacket(IPacket packet)
        {
            inputPackets.Add(packet);
        }

        //Long running task, quits when we finish consuming the collection
        async Task ProcessPacketQueue()
        {
            foreach (var pkt in inputPackets.GetConsumingEnumerable())
            {
                try
                {
                    await ProcessPacketDirect(pkt);
                }
                catch (Exception ex)
                {
                    FLLog.Error("Player", $"Exception thrown while processing packets. Force disconnect {Character?.Name ?? "null"}");
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
            if(packet is InputUpdatePacket p)
                Space?.World.InputsUpdate(this, p);
            else {
                FLLog.Info("Player", $"Disconnecting player, invalid packet type {packet.GetType()}");
                Client.Disconnect(DisconnectReason.ConnectionError);
                Disconnected();
            }
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
            using var c= Character.BeginTransaction();
            c.UpdateRank(Character.Rank + 1);
        }

        void IServerPlayer.RequestCharacterDB()
        {
            Client.SendPacket(new NewCharacterDBPacket()
            {
                Factions = Game.GameData.Ini.NewCharDB.Factions,
                Packages = Game.GameData.Ini.NewCharDB.Packages,
                Pilots = Game.GameData.Ini.NewCharDB.Pilots
            }, PacketDeliveryMethod.ReliableOrdered);
        }

        Task<bool> IServerPlayer.SelectCharacter(int index)
        {
            if (index >= 0 && index < CharacterList.Count)
            {
                var sc = CharacterList[index];
                FLLog.Info("Server", $"opening id {sc.Id}");
                if (!Game.CharactersInUse.Add(sc.Id)) {
                    FLLog.Info("Server", $"Character `{sc.Name}` is already in use");
                    return Task.FromResult(false);
                }
                BeginGame(NetCharacter.FromDb(sc.Id, Game), null);
                return Task.FromResult(true);
            }
            else
            {
                return Task.FromResult(false);
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

        Task<bool> IServerPlayer.CreateNewCharacter(string name, int index)
        {
            if (!Game.Database.NameInUse(name))
            {
                FLLog.Info("Player", $"New char: {name}");
                SelectableCharacter sel = null;
                long id = Game.Database.AddCharacter(playerGuid, (db) => {
                    NetCharacter.FromSaveGame(Game, Game.NewCharacter(name, index), db);
                });
                sel = NetCharacter.FromDb(id, Game).ToSelectable();
                CharacterList.Add(sel);
                Client.SendPacket(new AddCharacterPacket()
                {
                    Character = sel
                }, PacketDeliveryMethod.ReliableOrdered);
                return Task.FromResult(true);
            } else {
                FLLog.Info("Player", $"Char name in use: {name}");
                return Task.FromResult(false);
            }
        }

        public void SpawnDebris(int id, GameObjectKind kind, string archetype, string part, Transform3D tr, float mass)
        {
            rpcClient.SpawnDebris(id, kind, archetype, part, tr.Position, tr.Orientation, mass);
        }

        void UpdateCurrentReputations()
        {
            rpcClient.UpdateReputations(Character.Reputation.Reputations.Select(x => new NetReputation()
            {
                FactionHash = x.Key.CRC,
                Reputation = x.Value
            }).ToArray());
        }

        public void UpdateCurrentInventory() => rpcClient.UpdateInventory(Character.Credits, GetShipWorth(), Character.EncodeLoadout());

        public void ForceLand(string target)
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
            Space.Leave(true);
            Space = null;
            Dead = true;
            rpcClient.Killed();
            Base = Character.Base;
            System = Character.System;
            Position = Character.Position;
            Orientation = Character.Orientation;
        }


        void IServerPlayer.Respawn()
        {
            if (Dead)
            {
                Dead = false;
                if (Base != null) {
                    PlayerEnterBase();
                } else {
                    SpaceInitialSpawn(null);
                }
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

        public Task SaveSP(string path, string description, int ids, DateTime? timeStamp)
        {
            var completionSource = new TaskCompletionSource();
            saveActions.Enqueue(() =>
            {
                if (Character != null)
                {
                    using var c = Character.BeginTransaction();
                    c.UpdatePosition(Base, System, Position, Orientation);
                }
                SaveGame sg;
                lock (thns)
                {
                    sg = SaveWriter.CreateSave(Character, description, ids, timeStamp, Game.GameData, thns.Rtcs,
                        thns.Ambients, Story);
                }
                MissionRuntime.WriteActiveTriggers(sg);
                IniWriter.WriteIniFile(path, sg.ToIni());
                completionSource.SetResult();
            });
            return completionSource.Task;
        }


        void LoggedOut()
        {
            if (Character != null)
            {
                using var c = Character.BeginTransaction();
                c.UpdatePosition(Base, System, Position, Orientation);
                Space?.Leave(false);
                Space = null;
                foreach(var player in Game.AllPlayers.Where(x => x != this))
                    player.RpcClient.OnPlayerLeave(ID, Name);
                Game.CharactersInUse.Remove(Character.ID);
                Character = null;
            }
        }

        public void Disconnected()
        {
            if (packetQueueTask != null)
            {
                inputPackets.CompleteAdding();
                packetQueueTask.Wait(1000);
            }
            LoggedOut();
        }

        public void JumpTo(string system, string target)
        {
            rpcClient.StartJumpTunnel();
            FLLog.Debug("Player", $"Jumping to {system} - {target}");
            if(Space != null) Space.Leave(false);
            Space = null;

            var sys = Game.GameData.Systems.Get(system);
            Game.Worlds.RequestWorld(sys, (world) =>
            {
                Space = new SpacePlayer(world, this);
                var obj = sys.Objects.FirstOrDefault((o) =>
                {
                    return o.Nickname.Equals(target, StringComparison.OrdinalIgnoreCase);
                });
                System = system;
                Base = null;
                Position = Vector3.Zero;
                Orientation = Quaternion.Identity;
                if (obj == null) {
                    FLLog.Error("Server", $"Can't find target {target} to spawn player in {system}");
                }
                else {
                    Position = obj.Position;
                    Orientation = obj.Rotation;
                    Position = Vector3.Transform(new Vector3(0, 0, 500), Orientation) + obj.Position; //TODO: This is bad
                }
                Baseside = null;
                Base = null;
                world.EnqueueAction(() =>
                {
                    rpcClient.SpawnPlayer(ID, System, world.GameWorld.CrcTranslation.ToArray(), Objective, Position, Orientation, world.CurrentTick);
                    world.SpawnPlayer(this, Position, Orientation);
                    msnRuntime?.PlayerLaunch();
                    msnRuntime?.CheckMissionScript();
                    msnRuntime?.EnteredSpace();
                });
            }, msnPreload);
        }

        void IServerPlayer.Launch()
        {
            if (Character.Ship == null) {
                FLLog.Error("Server", $"{Name} cannot launch without a ship");
                return;
            }
            var b = Game.GameData.Bases.Get(Base);
            var sys = Game.GameData.Systems.Get(b.System);
            Game.Worlds.RequestWorld(sys, (world) =>
            {
                Space = new SpacePlayer(world, this);
                var obj = sys.Objects.FirstOrDefault((o) =>
                {
                    return (o.Dock != null &&
                            o.Dock.Kind == DockKinds.Base &&
                            o.Dock.Target.Equals(Base, StringComparison.OrdinalIgnoreCase));
                });
                System = b.System;
                Orientation = Quaternion.Identity;
                Position = Vector3.Zero;
                if (obj == null)
                {
                    FLLog.Error("Base", "Can't find object in " + sys.Nickname + " docking to " + b.Nickname);
                }
                else
                {
                    Position = obj.Position;
                    Orientation = obj.Rotation;
                    Position = Vector3.Transform(new Vector3(0, 0, 500), Orientation) + obj.Position; //TODO: This is bad
                }
                Baseside = null;
                Base = null;
                world.EnqueueAction(() =>
                {
                    rpcClient.SpawnPlayer(ID, System, world.GameWorld.CrcTranslation.ToArray(), Objective, Position, Orientation, world.CurrentTick);
                    world.SpawnPlayer(this, Position, Orientation);
                    msnRuntime?.PlayerLaunch();
                    msnRuntime?.CheckMissionScript();
                    msnRuntime?.EnteredSpace();
                });
            }, msnPreload);
        }
    }
}
