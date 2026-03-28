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
using LibreLancer.Client.Components;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.Interface;
using LibreLancer.Missions;
using LibreLancer.Net;
using LibreLancer.Net.Protocol;
using LibreLancer.Render;
using LibreLancer.Server;
using LibreLancer.Thn;
using LibreLancer.World;
using LibreLancer.World.Components;
using LibreLancer.Net.Protocol.RpcPackets;
using LibreLancer.Resources;
using LibreLancer.Sounds.VoiceLines;

namespace LibreLancer.Client
{
    public class CGameSession : IClientPlayer
    {
        public long Credits;
        public int CurrentRank;
        public ulong ShipWorth;
        public long NetWorth;
        public long NextLevelWorth;
        public Ship? PlayerShip;
        public PlayerStats Statistics = new();
        public List<NetCargo> Items = [];
        public List<StoryCutsceneIni> ActiveCutscenes = [];
        public Dictionary<uint, VisitFlags> Visits = new();
        public DynamicThn Thns = new();
        public FreelancerGame Game;
        public ReputationCollection PlayerReputations = new();
        public int PlayerNetID;
        public string? PlayerBase;
        public Vector3 PlayerPosition;
        public Quaternion PlayerOrientation;
        public bool Admin = false;
        public NewsArticle[] News = [];
        public ChatSource Chats = new();
        private IPacketConnection connection;
        private IServerPlayer rpcServer;
        private ISpacePlayer spaceRpc;
        private IBasesidePlayer baseRpc;
        private double playerTotalTime = 0;
        private DateTime playerSessionStart = DateTime.UtcNow;
        public NetObjective CurrentObjective;
        public IServerPlayer RpcServer => rpcServer;
        public IBasesidePlayer BaseRpc => baseRpc;
        public ISpacePlayer SpaceRpc => spaceRpc;
        public double WorldTime => WorldTick * (1 / 60.0f);

        public bool Multiplayer => connection is GameNetClient;
        private bool paused = false;

        public uint WorldTick = 0;

        public string PlayerSystem = null!;

        public CircularBuffer<int> UpdatePacketSizes = new(200);

        public EmbeddedServer? EmbeddedServer => connection as EmbeddedServer;
        private SpaceGameplay? spaceGameplay;
        private Queue<Action> gameplayActions = new();
        private Queue<Action> uiActions = new();
        private Queue<Action> audioActions = new();
        public Action? ObjectiveUpdated;

        public string? AutoSavePath { get; private set; }
        private CircularBuffer<PlayerMoveState> moveState = new(128);
        private CircularBuffer<SPUpdatePacket> oldPackets = new(1000);
        public UpdateAck Acks;

        private int tickSyncCounter = 0;
        public int LastTickOffset = 0;

        public SoldGood[] Goods = null!;
        public NetSoldShip[]? Ships = null!;
        private ObjNetId? scanId;
        private NetLoadout? scanLoadout;
        private UIInventoryItem[] scannedInventory = [];
        private readonly Queue<IPacket> updatePackets = new();
        private AllowedDocking? allowedDocking;

        private static int NEW_PLAYER = 393298;
        private static int DEPARTING_PLAYER = 393299;

        private string? newPlayerStr;
        private string? departingPlayerStr;

        public void Pause()
        {
            if (connection is not EmbeddedServer es)
            {
                return;
            }

            es.Server.LocalPlayer?.Space?.World?.Pause();
            paused = true;
        }

        public void Resume()
        {
            if (connection is not EmbeddedServer es)
            {
                return;
            }

            es.Server.LocalPlayer?.Space?.World?.Resume();
            paused = false;
        }

        public void Save(string description)
        {
            if (connection is EmbeddedServer es)
            {
                Game.Saves.AddFile(es.Save(description, false));
            }
        }

        void IClientPlayer.SPSetAutosave(string path)
        {
            AutoSavePath = path;
        }

        public CGameSession(FreelancerGame g, IPacketConnection connection)
        {
            Game = g;
            this.connection = connection;
            ResponseHandler = new NetResponseHandler();
            rpcServer = new RemoteServerPlayer(connection, ResponseHandler);
            spaceRpc = new RemoteSpacePlayer(connection, ResponseHandler);
            baseRpc = new RemoteBasesidePlayer(connection, ResponseHandler);
        }

        public void CutsceneUpdate(NetThnInfo info)
        {
            Thns.Unpack(info, Game.GameData);
            ActiveCutscenes = [];

            foreach (var path in Thns.Rtcs)
            {
                var rtc = new StoryCutsceneIni(Game.GameData.Items.Ini.Freelancer.DataPath + path.Script,
                    Game.GameData.VFS)
                {
                    RefPath = path.Script!
                };

                ActiveCutscenes.Add(rtc);
            }
        }

        public void FinishCutscene(StoryCutsceneIni cutscene)
        {
            ActiveCutscenes.Remove(cutscene);
            rpcServer.RTCComplete(cutscene.RefPath);
        }

        public void RoomEntered(string room, string bse)
        {
            rpcServer.OnLocationEnter(bse, room);
        }

        void IClientPlayer.UpdateStatistics(NetPlayerStatistics stats)
        {
            Statistics.TotalMissions = stats.TotalMissions;
            Statistics.TotalKills = (stats.FightersKilled + stats.FreightersKilled + stats.TransportsKilled +
                                     stats.BattleshipsKilled);
            Statistics.SystemsVisited = stats.SystemsVisited;
            Statistics.BasesVisited = stats.BasesVisited;
            Statistics.JumpHolesFound = stats.JumpHolesFound;
            Statistics.FightersKilled = stats.FightersKilled;
            Statistics.FreightersKilled = stats.FreightersKilled;
            Statistics.TransportsKilled = stats.TransportsKilled;
            Statistics.BattleshipsKilled = stats.BattleshipsKilled;
        }

        public double CharacterPlayTime => playerTotalTime + (DateTime.UtcNow - playerSessionStart).TotalSeconds;

        private bool hasChanged = false;

        private void SceneChangeRequired()
        {
            gameplayActions.Clear();

            if (PlayerBase != null)
            {
                Game.ChangeState(new RoomGameplay(Game, this, PlayerBase));
            }
            else
            {
                Acks = default;
                processUpdatePackets = false;
                spaceGameplay = new SpaceGameplay(Game, this);
                Game.ChangeState(spaceGameplay);
            }

            hasChanged = true;
        }

        public bool Update()
        {
            hasChanged = false;
            UpdatePackets();
            UIUpdate();
            return hasChanged;
        }

        private void UpdateAudio()
        {
            while (audioActions.TryDequeue(out var act))
                act();
        }

        private void UIUpdate()
        {
            while (uiActions.TryDequeue(out var act))
                act();
        }

        private struct PlayerMoveState
        {
            public uint Tick;
            public Vector3 Position;
            public Quaternion Orientation;
            public Vector3 Steering;
            public Vector3 AimPoint;
            public float Throttle;
            public StrafeControls Strafe;
            public bool Thrust;
            public bool CruiseEnabled;
            public ProjectileFireCommand? FireCommand;
        }

        private NetInputControls FromMoveState(int i)
        {
            i++;
            return new NetInputControls()
            {
                Tick = moveState[^i].Tick,
                Steering = moveState[^i].Steering,
                AimPoint = moveState[^i].AimPoint,
                Strafe = moveState[^i].Strafe,
                Throttle = moveState[^i].Throttle,
                Cruise = moveState[^i].CruiseEnabled,
                Thrust = moveState[^i].Thrust,
                FireCommand = moveState[^i].FireCommand,
            };
        }

        public void UpdateStart(SpaceGameplay gp)
        {
            var elapsed = (uint) ((Game.TotalTime - totalTimeForTick) / (1 / 60.0f));
            FLLog.Info("Player", $"{elapsed} ticks elapsed after load");
            WorldTick += elapsed;
        }

        public void GameplayUpdate(SpaceGameplay gp, double delta)
        {
            WorldTick++;
            UpdateAudio();

            while (gameplayActions.TryDequeue(out var act))
            {
                act();
            }

            if (!paused)
            {
                var player = gp.player;
                var phys = player.GetComponent<ShipPhysicsComponent>()!;
                var steering = player.GetComponent<ShipSteeringComponent>()!;
                var wp = player.GetComponent<WeaponControlComponent>()!;
                moveState.Enqueue(new PlayerMoveState()
                {
                    Tick = WorldTick,
                    Position = player.PhysicsComponent!.Body!.Position,
                    Orientation = player.PhysicsComponent.Body.Orientation,
                    Steering = steering.OutputSteering,
                    AimPoint = wp.AimPoint,
                    Strafe = phys.CurrentStrafe,
                    Throttle = phys.EnginePower,
                    Thrust = steering.Thrust,
                    CruiseEnabled = steering.Cruise,
                    FireCommand = gp.world.Projectiles!.GetQueuedRequest(),
                });

                // Store multiple updates for redundancy.
                var ip = new InputUpdatePacket()
                {
                    Current = FromMoveState(0),
                    Acks = Acks,
                };

                if (gp.Selection.Selected != null)
                {
                    ip.SelectedObject = gp.Selection.Selected;
                }

                if (moveState.Count > 1)
                {
                    ip.HistoryA = FromMoveState(1);
                }

                if (moveState.Count > 2)
                {
                    ip.HistoryB = FromMoveState(2);
                }

                if (moveState.Count > 3)
                {
                    ip.HistoryC = FromMoveState(3);
                }

                connection.SendPacket(ip, PacketDeliveryMethod.SequenceA);

                if (processUpdatePackets)
                {
                    List<SPUpdatePacket> toUpdate = [];

                    while (updatePackets.TryDequeue(out var pkt))
                    {
                        var sp = GetUpdatePacket(pkt);

                        if (sp != null)
                        {
                            toUpdate.Add(sp);
                        }
                    }

                    for (var i = 0; i < toUpdate.Count; i++)
                    {
                        // Only do resync on the last packet processed this frame
                        // Stops the resync spiral of death
                        ProcessUpdate(toUpdate[i], gp, i == toUpdate.Count - 1);
                    }

                    if (toUpdate.Count > 0)
                    {
                        ClockSync(toUpdate[^1]);
                    }
                }
            }

            (connection as GameNetClient)?.Update(); // Send packets at 60fps
        }

        private readonly MovingAverage<int> ticks = new(90);

        public int DroppedInputs = 0;
        public double AdjustedInterval = 1.0;
        public int AverageTickOffset => ticks.Average;

        private int jumpTimer = 0;

        private void ClockSync(SPUpdatePacket packet)
        {
            var tickOffset = (int) ((long) packet.InputSequence - (long) packet.Tick);
            LastTickOffset = tickOffset;
            jumpTimer--;

            if (jumpTimer < 0)
            {
                jumpTimer = 0;
            }

            if (tickOffset < -50 && jumpTimer == 0)
            {
                WorldTick += 32; // Jump ahead in time
                jumpTimer = 10;
                AdjustedInterval = 0.9687;
                return;
            }

            ticks.AddValue(tickOffset);

            if (tickOffset < 0)
            {
                ticks.ForceSetAverage(tickOffset);
                DroppedInputs++;
            }

            AdjustedInterval = ticks.Average switch
            {
                <= -16 => 0.75,
                <= -8 => 0.875,
                <= -4 => 0.92,
                < 0 => 0.9687,
                >= 16 => 1.205,
                >= 8 => 1.085,
                >= 6 => 1.0312,
                >= 4 => 1.0070,
                (>= 3) when !Multiplayer => 1.0050,
                _ => 1.0
            };
        }

        private ObjectUpdate GetUpdate(uint tick, int id)
        {
            for (var i = 0; i < oldPackets.Count; i++)
            {
                if (oldPackets[i].Tick != tick)
                {
                    continue;
                }

                foreach (var packet in oldPackets[i].Updates)
                {
                    if (packet.ID.Value == id)
                    {
                        return packet;
                    }
                }

                throw new Exception($"History {tick} missing id {id}");
            }

            throw new Exception($"History {tick} missing");
        }

        private SPUpdatePacket? GetUpdatePacket(IPacket p)
        {
            if (p is SPUpdatePacket sp)
            {
                return sp;
            }

            var mp = (PackedUpdatePacket) p;
            var oldPlayerState = new PlayerAuthState();

            if (mp.OldTick != 0)
            {
                int i;

                for (i = 0; i < oldPackets.Count; i++)
                {
                    if (oldPackets[i].Tick != mp.OldTick)
                    {
                        continue;
                    }

                    oldPlayerState = oldPackets[i].PlayerState;
                    break;
                }

                if (i == oldPackets.Count)
                {
                    FLLog.Error("Net", $"Unable to find old tick {mp.OldTick}, resetting ack");
                    Acks = default;
                    return null;
                }
            }

            UpdatePacketSizes.Enqueue(mp.DataSize);

            var updates = mp.GetUpdates(oldPlayerState, GetUpdate);
            var nsp = new SPUpdatePacket
            {
                Tick = mp.Tick,
                InputSequence = mp.InputSequence,
                PlayerState = updates.AuthState,
                Updates = updates.Updates
            };


            oldPackets.Enqueue(nsp);
            // Create new acknowledgement history
            var prevAcks = Acks;
            Acks = new UpdateAck(mp.Tick, 0);

            for (uint i = 1; i < 64; i++)
            {
                uint tick = mp.Tick - i;
                Acks[tick] = prevAcks[tick];
            }

            return nsp;
        }

        public DisplayFaction[] GetUIRelations()
        {
            return PlayerReputations.Reputations
                .Where(x => !x.Key.Hidden)
                .Select(x => new DisplayFaction(x.Key.IdsName, x.Value))
                .OrderBy(x => x.Relationship)
                .ToArray();
        }

        public int UpdateQueueCount => updatePackets.Count;

        private volatile bool processUpdatePackets = false;

        public void WorldReady()
        {
            spaceGameplay!.world.SetCrcTranslation(crcMap);

            while (gameplayActions.TryDequeue(out var act))
            {
                act();
            }
        }

        public void BeginUpdateProcess()
        {
            processUpdatePackets = true;
            moveState = new CircularBuffer<PlayerMoveState>(128);
        }

        private void Resimulate(int i, SpaceGameplay gameplay)
        {
            var physComponent = gameplay.player.GetComponent<ShipPhysicsComponent>();
            var player = gameplay.player;
            physComponent!.CurrentStrafe = moveState[i].Strafe;
            physComponent.EnginePower = moveState[i].Throttle;
            physComponent.Steering = moveState[i].Steering;
            physComponent.ThrustEnabled = moveState[i].Thrust;
            physComponent.Update(1 / 60.0f, gameplay.world);
            gameplay.player.PhysicsComponent!.Body!.PredictionStep(1 / 60.0f);
            moveState[i].Position = player.PhysicsComponent!.Body!.Position;
            moveState[i].Orientation = player.PhysicsComponent.Body.Orientation;
        }

        private void SmoothError(GameObject obj, Vector3 oldPos, Quaternion oldQuat)
        {
            var newPos = obj.PhysicsComponent!.Body!.Position;
            var newOrient = obj.PhysicsComponent.Body.Orientation;

            if ((oldPos - newPos).Length() >
                obj.PhysicsComponent.Body.LinearVelocity.Length() * 0.33f)
            {
                obj.PhysicsComponent.PredictionErrorPos = Vector3.Zero;
                obj.PhysicsComponent.PredictionErrorQuat = Quaternion.Identity;
            }
            else
            {
                obj.PhysicsComponent.PredictionErrorPos = (oldPos - newPos);
                obj.PhysicsComponent.PredictionErrorQuat =
                    Quaternion.Inverse(newOrient) * oldQuat;
            }
        }

        private void ProcessUpdate(SPUpdatePacket p, SpaceGameplay gp, bool resync)
        {
            foreach (var update in p.Updates)
                UpdateObject(update, gp.world);
            var hp = gp.player.GetComponent<CHealthComponent>();
            var state = p.PlayerState;

            if (hp != null)
            {
                hp.CurrentHealth = state.Health;
                var sh = gp.player.GetFirstChildComponent<CShieldComponent>();
                sh?.SetShieldHealth(state.Shield);
            }

            if (gp?.player == null || !resync)
            {
                return;
            }

            for (var i = moveState.Count - 1; i >= 0; i--)
            {
                if (moveState[i].Tick != p.Tick)
                {
                    continue;
                }

                var errorPos = state.Position - moveState[i].Position;
                var errorQuat = MathHelper.QuatError(state.Orientation, moveState[i].Orientation);
                var phys = gp.player.GetComponent<ShipPhysicsComponent>()!;

                if (p.PlayerState.CruiseAccelPct > 0 || p.PlayerState.CruiseChargePct > 0)
                {
                    phys.ResyncChargePercent(p.PlayerState.CruiseChargePct,
                        (1 / 60.0f) * (moveState.Count - i));
                    phys.ResyncCruiseAccel(p.PlayerState.CruiseAccelPct, (1 / 60.0f) * (moveState.Count - i));
                }

                if (errorPos.Length() > 0.1 || errorQuat > 0.1f)
                {
                    // We now do a basic resim without collision
                    // This needs some work to not show the errors in collision on screen
                    // for the client, but it's almost there
                    // This is much faster than stepping the entire simulation again
                    FLLog.Info("Client",
                        $"Applying correction at tick {p.InputSequence}. Errors ({errorPos.Length()},{errorQuat})");
                    var transform = gp.player.LocalTransform;
                    var predictedPos = transform.Position;
                    var predictedOrient = transform.Orientation;
                    moveState[i].Position = state.Position;
                    moveState[i].Orientation = state.Orientation;
                    // Set states
                    gp.player.SetLocalTransform(new Transform3D(state.Position, state.Orientation));
                    gp.player.PhysicsComponent!.Body!.LinearVelocity = state.LinearVelocity;
                    gp.player.PhysicsComponent.Body.AngularVelocity = state.AngularVelocity;
                    phys.ChargePercent = state.CruiseChargePct;
                    phys.CruiseAccelPct = state.CruiseAccelPct;

                    // simulate inputs - only outside a tradelane. we go back in time for a tradelane a bit
                    for (i = i + 1; i < moveState.Count; i++)
                    {
                        Resimulate(i, gp);
                    }

                    SmoothError(gp.player, predictedPos, predictedOrient);
                    gp.player.PhysicsComponent.Update(1 / 60.0, gp.world);
                }

                break;
            }
        }

        public Action<IPacket>? ExtraPackets;

        private NetCargo ResolveCargo(NetShipCargo cg)
        {
            var equip = Game.GameData.Items.Equipment.Get(cg.EquipCRC)!;
            return new NetCargo(cg.ID)
            {
                Equipment = equip,
                Hardpoint = cg.Hardpoint,
                Health = cg.Health / 255f,
                Count = cg.Count
            };
        }

        private void SetSelfLoadout(NetLoadout ld)
        {
            var sh = ld.ArchetypeCrc == 0 ? null : Game.GameData.Items.Ships.Get(ld.ArchetypeCrc);
            PlayerShip = sh;

            Items = new List<NetCargo>(ld.Items.Count);

            if (sh != null)
            {
                foreach (var cg in ld.Items)
                {
                    Items.Add(ResolveCargo(cg));
                }
            }
        }

        private bool inTradelane = false;

        void IClientPlayer.StartTradelane()
        {
            inTradelane = true;
            RunSync(spaceGameplay!.StartTradelane);
        }

        void IClientPlayer.UpdateVisits(VisitBundle bundle)
        {
            Visits = new();

            foreach (var b in bundle.Visits)
            {
                Visits[b.Obj.Hash] = (VisitFlags) b.Visit;
            }
        }

        void IClientPlayer.VisitObject(uint hash, byte flags)
        {
            Visits[hash] = (VisitFlags) flags;
        }

        public bool IsVisited(uint hash)
        {
            if (!Visits.TryGetValue(hash, out var visit))
            {
                return false;
            }

            return (visit & VisitFlags.Hidden) != VisitFlags.Hidden &&
                   (visit & VisitFlags.Visited) == VisitFlags.Visited;
        }

        void IClientPlayer.TradelaneDisrupted()
        {
            inTradelane = false;
            RunSync(spaceGameplay!.TradelaneDisrupted);
        }

        void IClientPlayer.EndTradelane()
        {
            inTradelane = false;
            RunSync(spaceGameplay!.EndTradelane);
        }

        void IClientPlayer.StartTractor(ObjNetId ship, ObjNetId target)
        {
            RunSync(() =>
            {
                var src = spaceGameplay!.world.GetObject(ship);
                var dst = spaceGameplay.world.GetObject(target);

                if (src != null &&
                    dst != null &&
                    src.TryGetComponent<CTractorComponent>(out var tractor))
                {
                    tractor.AddBeam(dst);
                }
            });
        }

        void IClientPlayer.EndTractor(ObjNetId ship, ObjNetId target)
        {
            RunSync(() =>
            {
                var src = spaceGameplay!.world.GetObject(ship);
                var dst = spaceGameplay.world.GetObject(target);

                if (src != null &&
                    dst != null &&
                    src.TryGetComponent<CTractorComponent>(out var tractor))
                {
                    tractor.RemoveBeam(dst);
                }
            });
        }

        void IClientPlayer.TractorFailed()
        {
            // empty
            Game.Sound.PlayVoiceLine(VoiceLines.NnVoiceName, VoiceLines.NnVoice.TractorFailure);
        }

        void IClientPlayer.UpdateLootObject(ObjNetId id, NetBasicCargo[] cargo)
        {
            var newCargo = cargo.Select(x
                    => new BasicCargo(Game.GameData.Items.Equipment.Get(x.EquipCRC)!, x.Count))
                .Where(x => x.Item != null)
                .ToList();
            RunSync(() =>
            {
                var loot = spaceGameplay!.world.GetObject(id)!;

                if (loot.TryGetComponent<LootComponent>(out var l))
                {
                    l.Cargo = newCargo;
                }
            });
        }

        void IClientPlayer.SpawnProjectiles(ProjectileSpawn[] projectiles)
        {
            RunSync(() =>
            {
                foreach (var p in projectiles)
                {
                    var owner = spaceGameplay!.world.GetObject(p.Owner);

                    if (owner == spaceGameplay.player)
                    {
                        continue;
                    }

                    if (owner != null && owner.TryGetComponent<WeaponControlComponent>(out var wc))
                    {
                        int tgtUnique = 0;

                        if (wc.NetOrderWeapons == null)
                        {
                            wc.UpdateNetWeapons();
                        }

                        for (int i = 0; i < wc.NetOrderWeapons!.Length; i++)
                        {
                            if ((p.Guns & (1UL << i)) == 0)
                            {
                                continue;
                            }

                            var target = p.Target;

                            if ((p.Unique & (1UL << i)) != 0)
                            {
                                target = p.OtherTargets[tgtUnique++];
                            }

                            wc.NetOrderWeapons[i].Fire(target, spaceGameplay.world, null, true);
                        }
                    }
                }
            });
        }

        public class Popup
        {
            public int Title;
            public int Contents;
            public required string ID;
        }

        public ConcurrentQueue<Popup> Popups = new();

        void IClientPlayer.PopupOpen(int title, int contents, string id)
        {
            FLLog.Debug("CGameSession", "Enqueuing popup");
            Popups.Enqueue(new Popup() { Title = title, Contents = contents, ID = id });
        }

        void IClientPlayer.StartJumpTunnel()
        {
            FLLog.Warning("Client", "Jump tunnel unimplemented");
        }

        void IClientPlayer.SetObjective(NetObjective objective, bool history)
        {
            CurrentObjective = objective;

            if (!history)
            {
                ObjectiveUpdated?.Invoke();
            }
        }

        void IClientPlayer.Killed()
        {
            RunSync(() => { spaceGameplay?.Killed(); });
        }

        void IClientPlayer.SpawnMissile(int id, bool playSound, uint equip, Vector3 position, Quaternion orientation)
        {
            RunSync(() =>
            {
                var eq = Game.GameData.Items.Equipment.Get(equip);

                if (eq is not MissileEquip mn)
                {
                    return;
                }

                var go = new GameObject(mn.ModelFile!.LoadFile(Game.ResourceManager)!,
                    Game.ResourceManager);
                go.SetLocalTransform(new Transform3D(position, orientation));
                go.NetID = id;
                go.Kind = GameObjectKind.Missile;
                go.PhysicsComponent?.Mass = 1;

                if (mn.Def.ConstEffect != null)
                {
                    var fx = Game.GameData.Items.Effects.Get(mn.Def.ConstEffect)?
                        .GetEffect(Game.ResourceManager);
                    var ren = new ParticleEffectRenderer(fx) { Attachment = go.GetHardpoint(mn.Def.HpTrailParent) };
                    go.ExtraRenderers.Add(ren);
                }

                go.AddComponent(new CMissileComponent(go, mn));
                spaceGameplay!.world.AddObject(go);
                go.Register(spaceGameplay!.world);
            });
        }

        void IClientPlayer.DestroyMissile(int id, bool explode)
        {
            RunSync(() =>
            {
                var despawn = spaceGameplay!.world.GetNetObject(id);

                if (despawn != null)
                {
                    if (explode && despawn.TryGetComponent<CMissileComponent>(out var ms)
                                && ms.Missile?.ExplodeFx != null)
                    {
                        spaceGameplay.world.Renderer!.SpawnTempFx(ms.Missile.ExplodeFx.GetEffect(Game.ResourceManager),
                            despawn.LocalTransform.Position);
                    }

                    despawn.Unregister(spaceGameplay.world);
                    spaceGameplay.world.RemoveObject(despawn);
                    FLLog.Debug("Client", $"Destroyed missile {id}");
                }
            });
        }

        // Use only for Single Player
        // Works because the data is already loaded,
        // and this is really only waiting for the embedded server to start
        private bool started = false;

        public void WaitStart()
        {
            if (!started)
            {
                while (connection.PollPacket(out var packet))
                {
                    HandlePacket(packet);

                    if (packet is IClientPlayer_BaseEnterPacket || packet is IClientPlayer_SpawnPlayerPacket)
                    {
                        started = true;
                    }
                }
            }
        }

        private int enterCount = 0;

        void IClientPlayer.OnConsoleMessage(string text)
        {
            FLLog.Info("Console", text);
            var msg = BinaryChatMessage.PlainText(text);

            if (text.Length > 200)
            {
                msg.Segments[0].Size = ChatMessageSize.Small;
            }

            Chats.Append(null, msg, Color4.LimeGreen, "Arial");
        }

        private void RunSync(Action gp) => gameplayActions.Enqueue(gp);

        public Action? OnUpdateInventory;
        public Action? OnUpdatePlayerShip;

        void IClientPlayer.UpdateReputations(NetReputation[] reps)
        {
            foreach (var r in reps)
            {
                var f = Game.GameData.Items.Factions.Get(r.FactionHash);

                if (f != null)
                {
                    PlayerReputations.Reputations[f] = r.Reputation;
                }
            }
        }

        private PlayerInventory lastInventory = new();

        void IClientPlayer.UpdateInventory(PlayerInventoryDiff diff)
        {
            lastInventory = diff.Apply(lastInventory);
            Credits = lastInventory.Credits;
            ShipWorth = lastInventory.ShipWorth;
            NetWorth = (long) lastInventory.NetWorth;
            SetSelfLoadout(lastInventory.Loadout);

            if (OnUpdateInventory == null)
            {
                return;
            }

            uiActions.Enqueue(OnUpdateInventory);

            if (spaceGameplay == null && OnUpdatePlayerShip != null)
            {
                uiActions.Enqueue(OnUpdatePlayerShip);
            }
        }

        void IClientPlayer.UpdateCharacterProgress(int rank, long nextNetWorth)
        {
            CurrentRank = rank;
            NextLevelWorth = nextNetWorth;
        }

        public void UpdateSlotCount(int slot, int count)
        {
            var cargo = Items.FirstOrDefault(x => x.ID == slot);
            cargo?.Count = count;

            if (OnUpdateInventory != null)
            {
                uiActions.Enqueue(OnUpdateInventory);
            }
        }

        public void DeleteSlot(int slot)
        {
            var cargo = Items.FirstOrDefault(x => x.ID == slot);

            if (cargo != null)
            {
                Items.Remove(cargo);
            }

            if (OnUpdateInventory != null)
            {
                uiActions.Enqueue(OnUpdateInventory);
            }
        }

        public void EnqueueAction(Action a) => uiActions.Enqueue(a);

        public void UpdateWeaponGroups(NetWeaponGroup[] wg)
        {
        }

        void IClientPlayer.UndockFrom(ObjNetId netId, int index)
        {
            RunSync(() =>
            {
                var obj = spaceGameplay!.world.GetObject(netId);

                if (obj == null)
                {
                    return;
                }

                if (obj.TryGetComponent<DockInfoComponent>(out var dock))
                {
                    spaceGameplay.SetDockCam(dock.GetDockCamera(index)!);
                }

                spaceGameplay.pilotComponent!.Undock(obj, index);
            });
        }

        void IClientPlayer.RunDirectives(MissionDirective[] directives)
        {
            FLLog.Debug("Client", "Received directives for player");
            RunSync(() => { spaceGameplay!.Directives.SetDirectives(directives, spaceGameplay.world); });
        }

        void IClientPlayer.SpawnObjects(ObjectSpawnInfo[] objects)
        {
            RunSync(() =>
            {
                foreach (var objInfo in objects)
                {
                    GameObject? newObj;

                    if ((objInfo.Flags & ObjectSpawnFlags.Debris) == ObjectSpawnFlags.Debris)
                    {
                        newObj = CreateDebris(objInfo);
                    }
                    else if ((objInfo.Flags & ObjectSpawnFlags.Solar) == ObjectSpawnFlags.Solar)
                    {
                        var solar = Game.GameData.Items.Archetypes.Get(objInfo.Loadout.ArchetypeCrc)!;
                        newObj = new GameObject(solar, null, Game.ResourceManager, true, true);

                        if (objInfo.Dock != null && solar.DockSpheres.Count > 0)
                        {
                            newObj.AddComponent(new DockInfoComponent(newObj)
                            {
                                Action = objInfo.Dock,
                                Spheres = solar.DockSpheres.ToArray()
                            });
                        }

                        if (solar.Hitpoints > 0)
                        {
                            newObj.AddComponent(new CHealthComponent(newObj)
                                { CurrentHealth = objInfo.Loadout.Health, MaxHealth = solar.Hitpoints });
                        }
                    }
                    else if ((objInfo.Flags & ObjectSpawnFlags.Loot) == ObjectSpawnFlags.Loot)
                    {
                        var crate =
                            (LootCrateEquipment) Game.GameData.Items.Equipment.Get(objInfo.Loadout.ArchetypeCrc)!;
                        var model = crate.ModelFile!.LoadFile(Game.ResourceManager)!;
                        newObj = new GameObject(model, Game.ResourceManager)
                        {
                            Kind = GameObjectKind.Loot,
                            PhysicsComponent =
                            {
                                Mass = crate.Mass
                            },
                            ArchetypeName = crate.Nickname
                        };
                        newObj.AddComponent(new CHealthComponent(newObj)
                            { MaxHealth = crate.Hitpoints, CurrentHealth = crate.Hitpoints });
                        newObj.Name = new LootName(newObj);
                    }
                    else
                    {
                        var shp = Game.GameData.Items.Ships.Get((int) objInfo.Loadout.ArchetypeCrc)!;
                        newObj = new GameObject(shp, Game.ResourceManager, true, true);
                        newObj.AddComponent(new CHealthComponent(newObj)
                            { CurrentHealth = objInfo.Loadout.Health, MaxHealth = shp.Hitpoints });
                        newObj.AddComponent(new CExplosionComponent(newObj, shp.Explosion!));
                    }

                    if (newObj is null)
                    {
                        FLLog.Warning("Client", "Unable to spawn new object");
                        return;
                    }

                    // disable parts
                    foreach (var p in objInfo.DestroyedParts)
                    {
                        newObj.DisableCmpPart(p, spaceGameplay.world, Game.ResourceManager, out _);
                    }

                    newObj.Name ??= objInfo.Name;
                    newObj.NetID = objInfo.ID.Value;
                    newObj.Nickname = objInfo.Nickname;
                    newObj.SetLocalTransform(new Transform3D(objInfo.Position, objInfo.Orientation));
                    var head = Game.GameData.Items.Bodyparts.Get(objInfo.CommHead);
                    var body = Game.GameData.Items.Bodyparts.Get(objInfo.CommBody);
                    var helmet = Game.GameData.Items.Accessories.Get(objInfo.CommHelmet);

                    if (head != null || body != null)
                    {
                        newObj.AddComponent(new CostumeComponent(newObj)
                        {
                            Head = head,
                            Body = body,
                            Helmet = helmet
                        });
                    }

                    var fac = Game.GameData.Items.Factions.Get(objInfo.Affiliation);

                    if (fac != null)
                    {
                        newObj.AddComponent(new CFactionComponent(newObj, fac));
                    }

                    if ((objInfo.Flags & ObjectSpawnFlags.Friendly) == ObjectSpawnFlags.Friendly)
                    {
                        newObj.Flags |= GameObjectFlags.Friendly;
                    }

                    if ((objInfo.Flags & ObjectSpawnFlags.Hostile) == ObjectSpawnFlags.Hostile)
                    {
                        newObj.Flags |= GameObjectFlags.Hostile;
                    }

                    if ((objInfo.Flags & ObjectSpawnFlags.Neutral) == ObjectSpawnFlags.Neutral)
                    {
                        newObj.Flags |= GameObjectFlags.Neutral;
                    }

                    if ((objInfo.Flags & ObjectSpawnFlags.Important) == ObjectSpawnFlags.Important)
                    {
                        newObj.Flags |= GameObjectFlags.Important;
                    }

                    foreach (var eq in objInfo.Loadout.Items.Where(x => !string.IsNullOrEmpty(x.Hardpoint)))
                    {
                        var equip = Game.GameData.Items.Equipment.Get(eq.EquipCRC);

                        if (equip == null)
                        {
                            continue;
                        }

                        EquipmentObjectManager.InstantiateEquipment(newObj, Game.ResourceManager, Game.Sound,
                            EquipmentType.LocalPlayer, eq.Hardpoint, equip);
                    }

                    if (newObj.Kind == GameObjectKind.Loot)
                    {
                        var lt = new LootComponent(newObj);
                        newObj.AddComponent(lt);

                        foreach (var eq in objInfo.Loadout.Items.Where(x => string.IsNullOrWhiteSpace(x.Hardpoint)))
                        {
                            var equip = Game.GameData.Items.Equipment.Get(eq.EquipCRC);

                            if (equip == null)
                            {
                                continue;
                            }

                            lt.Cargo.Add(new BasicCargo(equip, eq.Count));
                        }
                    }

                    spaceGameplay!.world.AddObject(newObj);
                    newObj.Register(spaceGameplay.world);

                    if ((objInfo.Flags & ObjectSpawnFlags.Debris) == ObjectSpawnFlags.Debris ||
                        (objInfo.Flags & ObjectSpawnFlags.Loot) == ObjectSpawnFlags.Loot)
                    {
                        newObj.PhysicsComponent!.Body.SetDamping(0.5f, 0.2f);
                    }
                    else
                    {
                        newObj.AddComponent(new WeaponControlComponent(newObj));
                    }

                    // add fx
                    if (objInfo.Effects is { Length: > 0 })
                    {
                        var fx = new CNetEffectsComponent(newObj);
                        newObj.AddComponent(fx);
                        fx.UpdateEffects(objInfo.Effects, spaceGameplay!.world);
                    }

                    FLLog.Debug("Client", $"Spawned {newObj.NetID}");
                }
            });
        }

        private double totalTimeForTick = 0;

        private CrcIdMap[] crcMap = [];

        void IClientPlayer.SpawnPlayer(int ID, string system, CrcIdMap[] crcMap, NetObjective objective,
            Vector3 position, Quaternion orientation, uint tick)
        {
            enterCount++;
            PlayerNetID = ID;
            PlayerBase = null;
            CurrentObjective = objective;
            FLLog.Info("Client", $"Spawning in {system}");
            PlayerSystem = system;
            PlayerPosition = position;
            PlayerOrientation = orientation;
            SceneChangeRequired();
            var delay = connection.EstimateTickDelay();
            FLLog.Info("Player", $"Spawning at {tick} + delay {delay}");
            WorldTick = tick + connection.EstimateTickDelay();
            totalTimeForTick = Game.TotalTime;
            this.crcMap = crcMap;
        }

        void IClientPlayer.UpdateAnimations(ObjNetId id, NetCmpAnimation[] animations)
        {
            RunSync(() => spaceGameplay!.world.GetObject(id)?.AnimationComponent?.UpdateAnimations(animations));
        }

        private GameObject? CreateDebris(ObjectSpawnInfo obj)
        {
            ModelResource? src;
            List<SeparablePart>? sep;
            float[]? lodranges;

            if ((obj.Flags & ObjectSpawnFlags.Solar) == ObjectSpawnFlags.Solar)
            {
                var solar = Game.GameData.Items.Archetypes.Get(obj.Loadout.ArchetypeCrc);
                sep = solar?.SeparableParts;
                src = solar?.ModelFile?.LoadFile(Game.ResourceManager);
                lodranges = solar?.LODRanges;
            }
            else
            {
                var ship = Game.GameData.Items.Ships.Get(obj.Loadout.ArchetypeCrc);
                sep = ship?.SeparableParts;
                src = ship?.ModelFile?.LoadFile(Game.ResourceManager);
                lodranges = ship?.LODRanges;
            }

            if (src is null || sep is null || lodranges is null)
            {
                return null;
            }

#pragma warning disable CS8670

            var collider = src.Collision;
            var mdl = ((IRigidModelFile) src.Drawable)?.CreateRigidModel(true, Game.ResourceManager);
            var newmodel = mdl!.Parts![obj.DebrisPart].CloneAsRoot(mdl);
            var partName = newmodel.Root.Name!;
            var sepInfo = sep.FirstOrDefault(x => x.Part.Equals(partName, StringComparison.OrdinalIgnoreCase));
            var go = new GameObject(newmodel, collider, partName, sepInfo?.Mass ?? 1, true)
            {
                Kind = GameObjectKind.Debris,
                Model =
                {
                    SeparableParts = sep
                }
            };

#pragma warning restore CS8670

            if (go.RenderComponent is ModelRenderer mr)
            {
                mr.LODRanges = lodranges;
            }

            // Child damage cap
            if (sepInfo is { ChildDamageCap: not null } &&
                go.Model.TryGetHardpoint(sepInfo.ChildDamageCapHardpoint, out var capHp))
            {
                var dcap = GameObject.WithModel(sepInfo.ChildDamageCap.Model!, true, Game.ResourceManager);
                dcap.Attachment = capHp;
                dcap.Parent = go;
                dcap.RenderComponent!.InheritCull = false;

                if (dcap.Model!.TryGetHardpoint("DpConnect", out var dpConnect))
                {
                    dcap.SetLocalTransform(dpConnect.Transform.Inverse());
                }

                go.Children.Add(dcap);
            }

            return go;
        }

        void IClientPlayer.BaseEnter(string _base, NetObjective objective, NetThnInfo thns, NewsArticle[] news,
            SoldGood[] goods, NetSoldShip[] ships)
        {
            if (enterCount > 0 && (connection is EmbeddedServer es))
            {
                var path = Game.GetSaveFolder();
                Directory.CreateDirectory(path);
                es.Save(null, true);
            }

            CurrentObjective = objective;
            enterCount++;
            PlayerBase = _base;
            News = news;
            Goods = goods;
            Ships = ships;
            SceneChangeRequired();
            CutsceneUpdate(thns);
        }

        public Dictionary<uint, ulong> BaselinePrices = new();

        void IClientPlayer.UpdateBaselinePrices(BaselinePriceBundle prices)
        {
            foreach (var p in prices.Prices)
                BaselinePrices[p.GoodCRC] = p.Price;
        }

        void IClientPlayer.UpdateThns(NetThnInfo thns)
        {
            CutsceneUpdate(thns);
        }

        void IClientPlayer.DespawnObject(int id, bool explode)
        {
            RunSync(() =>
            {
                var despawn = spaceGameplay!.world.GetNetObject(id);

                if (despawn != null)
                {
                    if (explode)
                    {
                        spaceGameplay.Explode(despawn);
                    }

                    despawn.Unregister(spaceGameplay.world);
                    spaceGameplay.world.RemoveObject(despawn);
                    FLLog.Debug("Client", $"Despawned {id}");
                }
                else
                {
                    FLLog.Warning("Client", $"Tried to despawn unknown {id}");
                }
            });
        }

        void IClientPlayer.PlaySound(string sound)
        {
            audioActions.Enqueue(() => Game.Sound.PlayOneShot(sound));
        }

        void IClientPlayer.DestroyPart(ObjNetId id, uint part)
        {
            RunSync(() => { spaceGameplay!.world.GetObject(id)?.DisableCmpPart(part, spaceGameplay!.world, Game.ResourceManager, out _); });
        }

        void IClientPlayer.RunMissionDialog(NetDlgLine[] lines)
        {
            RunSync(() => { RunDialog(lines); });
        }

        void IClientPlayer.StopShip()
        {
            FLLog.Debug("Mission", "StopShip() call received");
            RunSync(() => spaceGameplay!.StopShip());
        }

        void IClientPlayer.MarkImportant(int id, bool important)
        {
            RunSync(() =>
            {
                var o = spaceGameplay!.world.GetNetObject(id);

                if (o == null)
                {
                    FLLog.Warning("Client", $"Could not find obj {id} to mark as important");
                }
                else
                {
                    if (important)
                    {
                        o.Flags |= GameObjectFlags.Important;
                    }
                    else
                    {
                        o.Flags &= ~GameObjectFlags.Important;
                    }
                }
            });
        }

        void IClientPlayer.PlayMusic(string? music, float fade) => audioActions.Enqueue(() =>
        {
            if (string.IsNullOrWhiteSpace(music) ||
                music.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                Game.Sound.StopMusic(fade);
            }
            else
            {
                spaceGameplay?.RtcMusic = true;
                Game.Sound.PlayMusic(music, fade);
            }
        });

        private void RunDialog(NetDlgLine[] lines, int index = 0)
        {
            if (index >= lines.Length)
            {
                return;
            }

            if (lines[index].TargetIsPlayer)
            {
                var obj = spaceGameplay!.world.GetObject(new ObjNetId(lines[index].Source));

                if (obj != null)
                {
                    spaceGameplay.OpenComm(obj, lines[index].Voice!);
                }
            }

            Game.Sound.PlayVoiceLine(lines[index].Voice!, lines[index].Hash, () =>
            {
                RunSync(() =>
                {
                    rpcServer.LineSpoken(lines[index].Hash);

                    if (lines[index].TargetIsPlayer)
                    {
                        spaceGameplay!.ClearComm();
                    }

                    RunDialog(lines, index + 1);
                });
            });
        }

        private void UpdatePackets()
        {
            while (connection.PollPacket(out var packet))
            {
                HandlePacket(packet);
            }
        }

        void IClientPlayer.UpdateAttitude(ObjNetId id, RepAttitude attitude)
        {
            RunSync(() =>
            {
                var obj = spaceGameplay!.world.GetObject(id);

                if (obj != null)
                {
                    obj.Flags &= ~(GameObjectFlags.Reputations);

                    if (attitude == RepAttitude.Friendly)
                    {
                        obj.Flags |= GameObjectFlags.Friendly;
                    }

                    if (attitude == RepAttitude.Neutral)
                    {
                        obj.Flags |= GameObjectFlags.Neutral;
                    }

                    if (attitude == RepAttitude.Hostile)
                    {
                        obj.Flags |= GameObjectFlags.Hostile;
                    }
                }
            });
        }

        void IClientPlayer.UpdateEffects(ObjNetId id, SpawnedEffect[] effect)
        {
            RunSync(() =>
            {
                var obj = spaceGameplay!.world.GetObject(id);

                if (obj != null)
                {
                    if (!obj.TryGetComponent<CNetEffectsComponent>(out var fx))
                    {
                        fx = new CNetEffectsComponent(obj);
                        obj.AddComponent(fx);
                    }

                    fx.UpdateEffects(effect, spaceGameplay!.world);
                }
            });
        }

        public void SetDebug(bool on)
        {
            if (connection is EmbeddedServer es)
            {
                es.Server.SendDebugInfo = on;
            }
        }

        public string? GetSelectedDebugInfo()
        {
            if (connection is EmbeddedServer es)
            {
                return es.Server.DebugInfo;
            }

            return null;
        }

        public MissionRuntime.TriggerInfo[]? GetTriggerInfo()
        {
            if (connection is EmbeddedServer es)
            {
                return es.Server.LocalPlayer?.MissionRuntime?.ActiveTriggersInfo;
            }

            return null;
        }

        void IClientPlayer.CallThorn(string? thorn, ObjNetId mainObject)
        {
            RunSync(() =>
            {
                if (thorn == null)
                {
                    spaceGameplay?.Thn = null;
                }
                else
                {
                    var thn = new ThnScript(Game.GameData.VFS.ReadAllBytes(Game.GameData.Items.DataPath(thorn)!),
                        Game.GameData.Items.ThornReadCallback, thorn);
                    var mo = spaceGameplay?.world.GetObject(mainObject);

                    if (mo != null)
                    {
                        FLLog.Info("Client", "Found thorn mainObject");
                    }
                    else
                    {
                        FLLog.Info("Client", $"Did not find mainObject with ID `{mainObject}. Assume player`");
                        mo = spaceGameplay!.player;
                    }

                    spaceGameplay!.Thn = new Cutscene(new ThnScriptContext(null) { MainObject = mo }, spaceGameplay);
                    spaceGameplay.Thn.BeginScene(thn);
                }
            });
        }

        public NetResponseHandler ResponseHandler;

        public void HandlePacket(IPacket pkt)
        {
            if (ResponseHandler.HandlePacket(pkt))
            {
                return;
            }

            var hcp = GeneratedProtocol.HandleIClientPlayer(pkt, this, connection);
            hcp.Wait();

            if (hcp.Result)
            {
                return;
            }

            if (pkt is not SPUpdatePacket && pkt is not PackedUpdatePacket)
            {
                FLLog.Debug("Client", "Got packet of type " + pkt.GetType());
            }

            switch (pkt)
            {
                case SPUpdatePacket:
                case PackedUpdatePacket:
                    if (processUpdatePackets)
                    {
                        updatePackets.Enqueue(pkt);
                    }

                    break;
                default:
                    if (ExtraPackets != null)
                    {
                        ExtraPackets(pkt);
                    }
                    else
                    {
                        FLLog.Error("Network", "Unknown packet type " + pkt.GetType().ToString());
                    }

                    break;
            }
        }

        private void UpdateObject(ObjectUpdate update, GameWorld world)
        {
            GameObject? obj = world.GetObject(update.ID);

            if (obj == null)
            {
                return;
            }

            if (obj.TryGetComponent<CEngineComponent>(out var eng))
            {
                eng.Speed = update.Throttle;

                foreach (var comp in obj.GetChildComponents<CThrusterComponent>())
                {
                    comp.Enabled = (update.CruiseThrust == CruiseThrustState.Thrusting);
                }
            }

            if (obj.TryGetComponent<CHealthComponent>(out var health))
            {
                health.CurrentHealth = update.HullValue;
            }

            if (obj.TryGetFirstChildComponent<CShieldComponent>(out var sh))
            {
                sh.SetShieldHealth(update.ShieldValue);
            }

            if (obj.TryGetComponent<WeaponControlComponent>(out var weapons) && (update.Guns?.Length ?? 0) > 0)
            {
                if (weapons.NetOrderWeapons == null)
                {
                    weapons.UpdateNetWeapons();
                }

                weapons.SetRotations(update.Guns!);
            }

            if (obj.SystemObject != null)
            {
                return;
            }

            var oldPos = obj.LocalTransform.Position;
            var oldQuat = obj.LocalTransform.Orientation;
            obj.PhysicsComponent!.Body.LinearVelocity = update.LinearVelocity.Vector;
            obj.PhysicsComponent.Body.AngularVelocity = update.AngularVelocity.Vector;
            obj.PhysicsComponent.Body.Activate();
            obj.PhysicsComponent.Body.SetTransform(new Transform3D(update.Position, update.Orientation.Quaternion));
            SmoothError(obj, oldPos, oldQuat);
        }

        public void Launch() => rpcServer.Launch();

        private void AppendBlue(string text)
        {
            Chats.Append(null, BinaryChatMessage.PlainText(text), Color4.CornflowerBlue, "Arial");
        }

        public void OnChat(ChatCategory category, string str)
        {
            if (str.TrimEnd() == "/ping")
            {
                if (connection is GameNetClient nc)
                {
                    var stats = $"Ping: {nc.Ping}, Loss {nc.LossPercent}%";
                    AppendBlue(stats);
                    AppendBlue(
                        $"Sent: {DebugDrawing.SizeSuffix(nc.BytesSent)}, Received: {DebugDrawing.SizeSuffix(nc.BytesReceived)}");
                }
                else
                {
                    AppendBlue("Offline");
                }
            }
            else if (str.TrimEnd() == "/debug")
            {
                Game.Debug.Enabled = !Game.Debug.Enabled;
            }
            else if (str.TrimEnd() == "/pos")
            {
                ((IClientPlayer) this).OnConsoleMessage(spaceGameplay != null
                    ? spaceGameplay.player.LocalTransform.Position.ToString()
                    : "null");
            }
            else
            {
                BinaryChatMessage msg;

                if (str[0] == '/' || !Admin)
                {
                    msg = BinaryChatMessage.PlainText(str);
                }
                else
                {
                    msg = BinaryChatMessage.ParseBbCode(str);
                }

                rpcServer.ChatMessage(category, msg);
            }
        }

        void IClientPlayer.ListPlayers(bool isAdmin) =>
            Admin = isAdmin;

        void IClientPlayer.ReceiveChatMessage(ChatCategory category, BinaryChatMessage player,
            BinaryChatMessage message)
        {
            Chats.Append(player, message, category.GetColor(), "Arial");
        }

        void IClientPlayer.OnPlayerJoin(int id, string name)
        {
            if (newPlayerStr == null)
            {
                newPlayerStr = Game.GameData.GetInfocardText(NEW_PLAYER, Game.Fonts)!.TrimEnd('\n');
            }

            Chats.Append(null, BinaryChatMessage.PlainText($"{newPlayerStr}{name}"), Color4.DarkRed, "Arial");
        }

        void IClientPlayer.OnPlayerLeave(int id, string name)
        {
            if (departingPlayerStr == null)
            {
                departingPlayerStr = Game.GameData.GetInfocardText(DEPARTING_PLAYER, Game.Fonts)!.TrimEnd('\n');
            }

            Chats.Append(null, BinaryChatMessage.PlainText($"{departingPlayerStr}{name}"), Color4.DarkRed, "Arial");
        }

        void IClientPlayer.TradelaneActivate(uint id, bool left)
        {
            gameplayActions.Enqueue(() =>
            {
                if (!(spaceGameplay!.world.GetObject(id)?.TryGetComponent<CTradelaneComponent>(out var tl) ?? false))
                {
                    return;
                }

                if (left)
                {
                    tl.ActivateLeft();
                }
                else
                {
                    tl.ActivateRight();
                }
            });
        }

        void IClientPlayer.TradelaneDeactivate(uint id, bool left)
        {
            gameplayActions.Enqueue(() =>
            {
                if (!(spaceGameplay!.world.GetObject(id)?.TryGetComponent<CTradelaneComponent>(out var tl) ?? false))
                {
                    return;
                }

                if (left)
                {
                    tl.DeactivateLeft();
                }
                else
                {
                    tl.DeactivateRight();
                }
            });
        }

        void IClientPlayer.ClearScan()
        {
            scanLoadout = null;
            scanId = null;
            scannedInventory = [];
            gameplayActions.Enqueue(() => spaceGameplay!.ClearScan());
        }

        void IClientPlayer.UpdateScan(ObjNetId id, NetLoadoutDiff diff)
        {
            scanLoadout ??= new NetLoadout();
            scanId = id;
            scanLoadout = diff.Apply(scanLoadout);
            scannedInventory = BuildScanList(scanLoadout);
            gameplayActions.Enqueue(() => { spaceGameplay?.UpdateScan(); });
        }

        public static UIInventoryItem FromNetCargo(NetCargo item)
        {
            return new UIInventoryItem()
            {
                ID = item.ID,
                Count = item.Count,
                Icon = item.Equipment!.Good!.Ini.ItemIcon,
                Good = item.Equipment.Good.Ini.Nickname,
                IdsInfo = item.Equipment.IdsInfo,
                IdsName = item.Equipment.IdsName,
                Volume = item.Equipment.Volume,
                Combinable = item.Equipment.Good.Ini.Combinable,
                CanMount = false,
                Equipment = item.Equipment,
                Hardpoint = item.Hardpoint
            };
        }

        private UIInventoryItem[] BuildScanList(NetLoadout loadout)
        {
            var list = loadout.Items
                .Select(ResolveCargo)
                .Where(x => x.Equipment?.Good != null)
                .Select(FromNetCargo)
                .ToList();

            Trader.SortGoods(this, list);
            return list.ToArray();
        }

        public UIInventoryItem[] GetScannedInventory(string filter)
        {
            var predicate = Trader.GetFilter(filter);
            return scannedInventory.Where(x => predicate(x.Equipment!)).ToArray();
        }

        void IClientPlayer.UpdatePlayTime(double time, DateTime startTime)
        {
            playerSessionStart = startTime;
            playerTotalTime = time;
        }

        void IClientPlayer.StoryMissionFailed(int failedIds)
        {
            RunSync(() =>
            {
                spaceGameplay!.StoryFail(failedIds);
                Pause();
            });
        }

        private GameObject ObjOrPlayer(int id)
        {
            if (id == 0)
            {
                return spaceGameplay!.player;
            }

            return spaceGameplay!.world.GetNetObject(id)!;
        }

        void IClientPlayer.UpdateFormation(NetFormation form)
        {
            if (spaceGameplay?.pilotComponent is null)
            {
                return;
            }

            gameplayActions.Enqueue(() =>
            {
                if (!form.Exists)
                {
                    FLLog.Debug("Client", "Formation cleared");
                    spaceGameplay.player.Formation = null;

                    if (spaceGameplay.pilotComponent.CurrentBehavior == AutopilotBehaviors.Formation)
                    {
                        spaceGameplay.pilotComponent.Cancel();
                    }
                }
                else
                {
                    FLLog.Debug("Client", "Formation received");
                    spaceGameplay.player.Formation = new ShipFormation(
                        ObjOrPlayer(form.LeadShip),
                        form.Followers.Select(ObjOrPlayer).ToArray()
                    )
                    {
                        PlayerPosition = form.YourPosition
                    };

                    FLLog.Debug("Client", $"Formation offset {form.YourPosition}");

                    if (spaceGameplay.player.Formation.LeadShip != spaceGameplay.player)
                    {
                        FLLog.Debug("Client", "Starting follow");
                        spaceGameplay.pilotComponent.StartFormation();
                    }
                }
            });
        }

        void IClientPlayer.UpdateAllowedDocking(AllowedDocking docking)
        {
            this.allowedDocking = docking;
        }

        public bool DockAllowed(GameObject gameObject)
        {
            if (allowedDocking == null)
            {
                return true;
            }

            if (!allowedDocking.CanTl)
            {
                if (allowedDocking.TlExceptions.Contains(gameObject.NicknameCRC))
                {
                    return true;
                }

                if (gameObject.TryGetComponent<DockInfoComponent>(out var dockInfo) &&
                    dockInfo.Action.Kind == DockKinds.Tradelane)
                {
                    return false;
                }
            }

            if (allowedDocking.CanDock)
            {
                return true;
            }

            if (allowedDocking.DockExceptions.Contains(gameObject.NicknameCRC))
            {
                return true;
            }

            return !gameObject.TryGetComponent<DockInfoComponent>(out var di) || di.Action.Kind == DockKinds.Tradelane;
        }

        public void Disconnected()
        {
            Game.ChangeState(new LuaMenu(Game));
        }

        public void QuitToMenu()
        {
            connection.Shutdown();
            Game.ChangeState(new LuaMenu(Game));
        }

        public void OnExit()
        {
            connection.Shutdown();
        }
    }
}
