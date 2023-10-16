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
using LibreLancer.Data.Missions;
using LibreLancer.GameData.Items;
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

namespace LibreLancer.Client
{
    public class EquipMount
    {
        public string Hardpoint;
        public string Item;

        public EquipMount(string hp, string item)
        {
            Hardpoint = hp;
            Item = item;
        }
    }


    public class CGameSession : IClientPlayer
	{
        public long Credits;
        public ulong ShipWorth;
		public string PlayerShip;
        public int CargoSize;
		public List<string> PlayerComponents = new List<string>();
        public List<NetCargo> Items = new List<NetCargo>();
        public List<StoryCutsceneIni> ActiveCutscenes = new List<StoryCutsceneIni>();
		public FreelancerGame Game;
		public string PlayerSystem;
        public ReputationCollection PlayerReputations = new ReputationCollection();
        public int PlayerNetID;
        public string PlayerBase;
		public Vector3 PlayerPosition;
		public Quaternion PlayerOrientation;
        public bool Admin = false;
        public NewsArticle[] News = new NewsArticle[0];
        public ChatSource Chats = new ChatSource();
        private IPacketConnection connection;
        private IServerPlayer rpcServer;
        public NetObjective CurrentObjective;
        public IServerPlayer RpcServer => rpcServer;
        public double WorldTime => WorldTick * (1 / 60.0f);

        public bool Multiplayer => connection is GameNetClient;
        private bool paused = false;

        public uint WorldTick = 0;

        public CircularBuffer<int> UpdatePacketSizes = new CircularBuffer<int>(200);

        public EmbeddedServer EmbedddedServer => connection as EmbeddedServer;

        public void Pause()
        {
            if (connection is EmbeddedServer es)
            {
                es.Server.LocalPlayer.World?.Pause();
                paused = true;
            }
        }

        public void Resume()
        {
            if (connection is EmbeddedServer es)
            {
                es.Server.LocalPlayer.World?.Resume();
                paused = false;
            }
        }

        private const string SAVE_ALPHABET = "0123456789abcdefghijklmnopqrstuvwxyz";
        static string Encode(long number)
        {
            if(number < 0)
                throw new ArgumentException();
            var builder = new StringBuilder();
            var divisor = (long) SAVE_ALPHABET.Length;
            while (number > 0)
            {
                number = Math.DivRem(number, divisor, out var rem);
                builder.Append(SAVE_ALPHABET[(int) rem]);
            }
            return new string(builder.ToString().Reverse().ToArray());
        }

        public void Save(string description)
        {
            var filename = $"Save0{Encode(DateTimeOffset.Now.ToUnixTimeSeconds())}.fl";
            if (string.IsNullOrWhiteSpace(description)) description = "Save";
            var folder = Game.GetSaveFolder();
            var path = Path.Combine(folder, filename);
            int i = 0;
            while (File.Exists(path)) {
                filename = $"Save0{Encode(DateTimeOffset.Now.ToUnixTimeSeconds())}{i++}.fl";
                path = Path.Combine(folder, filename);
            }
            if (connection is EmbeddedServer es)
            {
                es.Save(path, description, false);
                Game.Saves.AddFile(path);
            }
        }

        public CGameSession(FreelancerGame g, IPacketConnection connection)
		{
			Game = g;
            this.connection = connection;
            ResponseHandler = new NetResponseHandler();
            rpcServer = new RemoteServerPlayer(connection, ResponseHandler);
        }

        public void AddRTC(string[] paths)
        {
            if (paths == null) return;
            ActiveCutscenes = new List<StoryCutsceneIni>();
            foreach (var path in paths)
            {
                var rtc = new StoryCutsceneIni(Game.GameData.Ini.Freelancer.DataPath + path, Game.GameData.VFS);
                rtc.RefPath = path;
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

        private bool hasChanged = false;
        void SceneChangeRequired()
        {
            gameplayActions.Clear();
            if (PlayerBase != null)
            {
                Game.ChangeState(new RoomGameplay(Game, this, PlayerBase));
                hasChanged = true;
            }
            else
            {
                LastAck = 0;
                processUpdatePackets = false;
                gp = new SpaceGameplay(Game, this);
                Game.ChangeState(gp);
                hasChanged = true;
            }
        }

        SpaceGameplay gp;

        public bool Update()
        {
            hasChanged = false;
            UpdatePackets();
            UIUpdate();
            return hasChanged;
        }

        Queue<Action> gameplayActions = new Queue<Action>();
        private Queue<Action> uiActions = new Queue<Action>();
        private Queue<Action> audioActions = new Queue<Action>();

        void UpdateAudio()
        {
            while (audioActions.TryDequeue(out var act))
                act();
        }

        void UIUpdate()
        {
            while (uiActions.TryDequeue(out var act))
                act();
        }

        private CircularBuffer<PlayerMoveState> moveState = new CircularBuffer<PlayerMoveState>(128);
        private CircularBuffer<SPUpdatePacket> oldPackets = new CircularBuffer<SPUpdatePacket>(1000);
        public uint LastAck = 0;

        struct PlayerMoveState
        {
            public uint Tick;
            public Vector3 Position;
            public Quaternion Orientation;
            public Vector3 AngularVelocity;
            public Vector3 LinearVelocity;
            public Vector3 Steering;
            public float Throttle;
            public StrafeControls Strafe;
            public bool Thrust;
            public bool CruiseEnabled;
            public EngineStates EngineState;
            public float CruiseAccelPct;
            public float ChargePct;
        }

        NetInputControls FromMoveState(int i)
        {
            i++;
            return new NetInputControls()
            {
                Tick =  moveState[^i].Tick,
                Steering = moveState[^i].Steering,
                Strafe = moveState[^i].Strafe,
                Throttle = moveState[^i].Throttle,
                Cruise = moveState[^i].CruiseEnabled,
                Thrust = moveState[^i].Thrust
            };
        }

        private int tickSyncCounter = 0;

        public int LastTickOffset = 0;

        public void UpdateStart()
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
                act();
            if (!paused)
            {
                var player = gp.player;
                var phys = player.GetComponent<ShipPhysicsComponent>();
                var steering = player.GetComponent<ShipSteeringComponent>();

                moveState.Enqueue(new PlayerMoveState()
                {
                    Tick = WorldTick,
                    Position = player.PhysicsComponent.Body.Position,
                    Orientation = player.PhysicsComponent.Body.Transform.ExtractRotation(),
                    AngularVelocity = MathHelper.ApplyEpsilon(player.PhysicsComponent.Body.AngularVelocity),
                    LinearVelocity = player.PhysicsComponent.Body.LinearVelocity,
                    Steering = steering.OutputSteering,
                    Strafe = phys.CurrentStrafe,
                    Throttle = phys.EnginePower,
                    Thrust = steering.Thrust,
                    CruiseEnabled = steering.Cruise,
                    EngineState = phys.EngineState,
                    CruiseAccelPct = phys.CruiseAccelPct,
                    ChargePct = phys.ChargePercent
                });

                //Store multiple updates for redundancy.
                var ip = new InputUpdatePacket()
                {
                    Current = FromMoveState(0),
                    AckTick = LastAck,
                };
                if (gp.Selection.Selected != null)
                {
                    ip.SelectedObject = gp.Selection.Selected;
                }
                if (moveState.Count > 1) ip.HistoryA = FromMoveState(1);
                if (moveState.Count > 2) ip.HistoryB = FromMoveState(2);
                if (moveState.Count > 3) ip.HistoryC = FromMoveState(3);
                connection.SendPacket(ip, PacketDeliveryMethod.SequenceA);

                if (processUpdatePackets)
                {
                    List<SPUpdatePacket> toUpdate = new List<SPUpdatePacket>();
                    while (updatePackets.TryDequeue(out var pkt))
                    {
                        var sp = GetUpdatePacket(pkt);
                        if(sp != null)
                            toUpdate.Add(sp);
                    }

                    for (int i = 0; i < toUpdate.Count; i++) {
                        //Only do resync on the last packet processed this frame
                        //Stops the resync spiral of death
                        ProcessUpdate(toUpdate[i], gp, i == toUpdate.Count - 1);
                    }
                    if(toUpdate.Count > 0)
                        ClockSync(toUpdate[^1]);
                }
            }
            (connection as GameNetClient)?.Update(); //Send packets at 60fps
        }

        private MovingAverage<int> ticks = new MovingAverage<int>(90);

        public int DroppedInputs = 0;
        public double AdjustedInterval = 1.0;
        public int AverageTickOffset => ticks.Average;

        private int jumpTimer = 0;
        void ClockSync(SPUpdatePacket packet)
        {
            var tickOffset = (int)((long)packet.InputSequence - (long)packet.Tick);
            LastTickOffset = tickOffset;
            jumpTimer--;
            if (jumpTimer < 0) jumpTimer = 0;
            if (tickOffset < -50 && jumpTimer == 0) {
                WorldTick += 32; //Jump ahead in time
                jumpTimer = 10;
                AdjustedInterval = 0.9687;
                return;
            }
            ticks.AddValue(tickOffset);
            if (tickOffset < 0) {
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

        SPUpdatePacket GetUpdatePacket(IPacket p)
        {
            if (p is SPUpdatePacket sp) return sp;
            var mp = (PackedUpdatePacket) p;
            var oldPlayerState = new PlayerAuthState();
            var oldUpdates = Array.Empty<ObjectUpdate>();
            if (mp.OldTick != 0)
            {
                int i;
                for (i = 0; i < oldPackets.Count; i++) {
                    if (oldPackets[i].Tick == mp.OldTick)
                    {
                        oldPlayerState = oldPackets[i].PlayerState;
                        oldUpdates = oldPackets[i].Updates;
                        break;
                    }
                }
                if (i == oldPackets.Count) {
                    FLLog.Error("Net", $"Unable to find old tick {mp.OldTick}, resetting ack");
                    LastAck = 0;
                    return null;
                }
            }

            UpdatePacketSizes.Enqueue(mp.DataSize);
            var nsp = new SPUpdatePacket();
            nsp.Tick = mp.Tick;
            nsp.InputSequence = mp.InputSequence;
            (nsp.PlayerState, nsp.Updates) =
                mp.GetUpdates(oldPlayerState, oldUpdates, (connection as GameNetClient).HpidReader);
            oldPackets.Enqueue(nsp);
            LastAck = mp.Tick;
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

        volatile bool processUpdatePackets = false;


        public void WorldReady()
        {
            while (gameplayActions.TryDequeue(out var act))
                act();
        }

        public void BeginUpdateProcess()
        {
            processUpdatePackets = true;
            moveState = new CircularBuffer<PlayerMoveState>(128);
        }

        private Queue<IPacket> updatePackets = new Queue<IPacket>();



        void Resimulate(int i, SpaceGameplay gp)
        {
            var physComponent = gp.player.GetComponent<ShipPhysicsComponent>();
            var player = gp.player;
            physComponent.CurrentStrafe = moveState[i].Strafe;
            physComponent.EnginePower = moveState[i].Throttle;
            physComponent.Steering = moveState[i].Steering;
            physComponent.ThrustEnabled = moveState[i].Thrust;
            physComponent.Update(1 / 60.0f);
            gp.player.PhysicsComponent.Body.PredictionStep(1 / 60.0f);
            moveState[i].Position = player.PhysicsComponent.Body.Position;
            moveState[i].Orientation = player.PhysicsComponent.Body.Transform.ExtractRotation();
        }

        void SmoothError(GameObject obj, Vector3 oldPos, Quaternion oldQuat)
        {
            var newPos = obj.PhysicsComponent.Body.Position;
            var newOrient = obj.PhysicsComponent.Body.Transform.ExtractRotation();
            if ((oldPos - newPos).Length() >
                obj.PhysicsComponent.Body.LinearVelocity.Length() * 0.33f) {
                obj.PhysicsComponent.PredictionErrorPos = Vector3.Zero;
                obj.PhysicsComponent.PredictionErrorQuat = Quaternion.Identity;
            }
            else {
                obj.PhysicsComponent.PredictionErrorPos = (oldPos - newPos);
                obj.PhysicsComponent.PredictionErrorQuat =
                    Quaternion.Inverse(newOrient) * oldQuat;
            }
        }

        void ProcessUpdate(SPUpdatePacket p, SpaceGameplay gp, bool resync)
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
            if(gp?.player != null && resync)
            {
                for (int i = moveState.Count - 1; i >= 0; i--)
                {
                    if (moveState[i].Tick == p.Tick)
                    {
                        var errorPos = state.Position - moveState[i].Position;
                        var errorQuat = MathHelper.QuatError(state.Orientation, moveState[i].Orientation);
                        var phys = gp.player.GetComponent<ShipPhysicsComponent>();

                        if (p.PlayerState.CruiseAccelPct > 0 || p.PlayerState.CruiseChargePct > 0) {
                            phys.ResyncChargePercent(p.PlayerState.CruiseChargePct, (1 / 60.0f) * (moveState.Count - i));
                            phys.ResyncCruiseAccel(p.PlayerState.CruiseAccelPct, (1 / 60.0f) * (moveState.Count - i));
                        }

                        if (errorPos.Length() > 0.1 || errorQuat > 0.1f)
                        {
                            // We now do a basic resim without collision
                            // This needs some work to not show the errors in collision on screen
                            // for the client, but it's almost there
                            // This is much faster than stepping the entire simulation again
                            FLLog.Info("Client", $"Applying correction at tick {p.InputSequence}. Errors ({errorPos.Length()},{errorQuat})");
                            var tr = gp.player.LocalTransform;
                            var predictedPos = Vector3.Transform(Vector3.Zero, tr);
                            var predictedOrient = tr.ExtractRotation();
                            moveState[i].Position = state.Position;
                            moveState[i].Orientation = state.Orientation;
                            //Set states
                            gp.player.SetLocalTransform(Matrix4x4.CreateFromQuaternion(state.Orientation) * Matrix4x4.CreateTranslation(state.Position));
                            gp.player.PhysicsComponent.Body.LinearVelocity = state.LinearVelocity;
                            gp.player.PhysicsComponent.Body.AngularVelocity = state.AngularVelocity;
                            phys.ChargePercent = state.CruiseChargePct;
                            phys.CruiseAccelPct = state.CruiseAccelPct;
                            //simulate inputs - only outside a tradelane. we go back in time for a tradelane a bit
                            for (i = i + 1; i < moveState.Count; i++)
                            {
                                Resimulate(i, gp);
                            }
                            SmoothError(gp.player, predictedPos, predictedOrient);
                            gp.player.PhysicsComponent.Update(1 / 60.0);
                        }
                        break;

                    }
                }
            }

        }

        public Action<IPacket> ExtraPackets;


        void SetSelfLoadout(NetShipLoadout ld)
        {
            var sh = ld.ShipCRC == 0 ? null : Game.GameData.Ships.Get(ld.ShipCRC);
            PlayerShip = sh?.Nickname ?? null;
            CargoSize = sh?.HoldSize ?? 0;

            Items = new List<NetCargo>(ld.Items.Count);
            if (sh != null)
            {
                foreach (var cg in ld.Items)
                {
                    var equip = Game.GameData.Equipment.Get(cg.EquipCRC);
                    Items.Add(new NetCargo(cg.ID)
                    {
                        Equipment = equip,
                        Hardpoint = cg.Hardpoint,
                        Health = cg.Health / 255f,
                        Count = cg.Count
                    });
                }
            }
        }

        private bool inTradelane = false;

        void IClientPlayer.StartTradelane()
        {
            inTradelane = true;
            RunSync(gp.StartTradelane);
        }

        void IClientPlayer.TradelaneDisrupted()
        {
            inTradelane = false;
            RunSync(gp.TradelaneDisrupted);
        }

        void IClientPlayer.EndTradelane()
        {
            inTradelane = false;
            RunSync(gp.EndTradelane);
        }


        void IClientPlayer.SpawnProjectiles(ProjectileSpawn[] projectiles)
        {
            RunSync(() =>
            {
                foreach (var p in projectiles)
                {
                    var x = Game.GameData.Equipment.Get(p.Gun) as GunEquipment;
                    var projdata = gp.world.Projectiles.GetData(x);
                    var owner = gp.world.GetObject(p.Owner);
                    gp.world.Projectiles.SpawnProjectile(owner, p.Hardpoint, projdata, p.Start, p.Heading);
                    var o = owner.Children.FirstOrDefault(go =>
                        p.Hardpoint.Equals(go.Attachment?.Name));
                    if(o?.TryGetComponent<CMuzzleFlashComponent>(out var mz) ?? false)
                        mz.OnFired();
                }
            });
        }

        public class Popup
        {
            public int Title;
            public int Contents;
            public string ID;
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

        public Action ObjectiveUpdated;

        void IClientPlayer.SetObjective(NetObjective objective)
        {
            CurrentObjective = objective;
            ObjectiveUpdated?.Invoke();
        }

        void IClientPlayer.Killed()
        {
            RunSync(() =>
            {
                gp.Killed();
            });
        }

        void IClientPlayer.SpawnMissile(int id, bool playSound, uint equip, Vector3 position, Quaternion orientation)
        {
            RunSync(() =>
            {
                var eq = Game.GameData.Equipment.Get(equip);
                if (eq is MissileEquip mn)
                {
                    var go = new GameObject(mn.ModelFile.LoadFile(Game.ResourceManager),
                        Game.ResourceManager);
                    go.SetLocalTransform(Matrix4x4.CreateFromQuaternion(orientation) *
                                         Matrix4x4.CreateTranslation(position));
                    go.NetID = id;
                    go.Kind = GameObjectKind.Missile;
                    go.PhysicsComponent.Mass = 1;
                    go.World = gp.world;
                    if (mn.Def.ConstEffect != null)
                    {
                       var fx = Game.GameData.GetEffect(mn.Def.ConstEffect)?
                            .GetEffect(Game.ResourceManager);
                       var ren = new ParticleEffectRenderer(fx) {Attachment = go.GetHardpoint(mn.Def.HpTrailParent) };
                       go.ExtraRenderers.Add(ren);
                    }
                    go.AddComponent(new CMissileComponent(go, mn));
                    go.Register(go.World.Physics);
                    gp.world.AddObject(go);
                }
            });
        }

        void IClientPlayer.DestroyMissile(int id, bool explode)
        {
            RunSync(() =>
            {
                var despawn = gp.world.GetNetObject(id);
                if (despawn != null)
                {
                    if (explode && despawn.TryGetComponent<CMissileComponent>(out var ms)
                        && ms.Missile?.ExplodeFx != null)
                    {
                        var pos = Vector3.Transform(Vector3.Zero, despawn.LocalTransform);
                        gp.world.Renderer.SpawnTempFx(ms.Missile.ExplodeFx.GetEffect(Game.ResourceManager), pos);
                    }
                    despawn.Unregister(gp.world.Physics);
                    gp.world.RemoveObject(despawn);
                    FLLog.Debug("Client", $"Destroyed missile {id}");
                }
            });
        }

        //Use only for Single Player
        //Works because the data is already loaded,
        //and this is really only waiting for the embedded server to start
        private bool started = false;
        public void WaitStart()
        {
            IPacket packet;
            if (!started)
            {
                while (connection.PollPacket(out packet))
                {
                    HandlePacket(packet);
                    if (packet is IClientPlayer_BaseEnterPacket || packet is IClientPlayer_SpawnPlayerPacket)
                        started = true;
                }
            }
        }

        private int enterCount = 0;

        void IClientPlayer.OnConsoleMessage(string text)
        {
            Chats.Append(null, BinaryChatMessage.PlainText(text), Color4.LimeGreen, "Arial");
        }

        void RunSync(Action gp) => gameplayActions.Enqueue(gp);

        public Action OnUpdateInventory;
        public Action OnUpdatePlayerShip;

        void IClientPlayer.UpdateReputations(NetReputation[] reps)
        {
            foreach (var r in reps)
            {
                var f = Game.GameData.Factions.Get(r.FactionHash);
                if (f != null)
                    PlayerReputations.Reputations[f] = r.Reputation;
            }
        }

        void IClientPlayer.UpdateInventory(long credits, ulong shipWorth, NetShipLoadout loadout)
        {
            Credits = credits;
            ShipWorth = shipWorth;
            SetSelfLoadout(loadout);
            if (OnUpdateInventory != null) {
                uiActions.Enqueue(OnUpdateInventory);
                if(gp == null && OnUpdatePlayerShip != null)
                    uiActions.Enqueue(OnUpdatePlayerShip);
            }
        }

        public void UpdateSlotCount(int slot, int count)
        {
            var cargo = Items.FirstOrDefault(x => x.ID == slot);
            if (cargo != null)
                cargo.Count = count;
            if (OnUpdateInventory != null) uiActions.Enqueue(OnUpdateInventory);
        }

        public void DeleteSlot(int slot)
        {
            var cargo = Items.FirstOrDefault(x => x.ID == slot);
            if (cargo != null)
                Items.Remove(cargo);
            if (OnUpdateInventory != null) uiActions.Enqueue(OnUpdateInventory);
        }

        public void EnqueueAction(Action a) => uiActions.Enqueue(a);

        void IClientPlayer.SpawnObject(int id, ObjectName name, string affiliation, Vector3 position, Quaternion orientation, NetShipLoadout loadout)
        {
            RunSync(() =>
            {
                var shp = Game.GameData.Ships.Get((int) loadout.ShipCRC);
                var newobj = new GameObject(shp, Game.ResourceManager, true, true) {
                    World = gp.world
                };
                newobj.Name = name;
                newobj.NetID = id;
                newobj.SetLocalTransform(Matrix4x4.CreateFromQuaternion(orientation) *
                                         Matrix4x4.CreateTranslation(position));
                newobj.AddComponent(new CHealthComponent(newobj) { CurrentHealth = loadout.Health, MaxHealth = shp.Hitpoints });
                newobj.AddComponent(new CExplosionComponent(newobj, shp.Explosion));
                var fac = Game.GameData.Factions.Get(affiliation);
                if(fac != null)
                    newobj.AddComponent(new CFactionComponent(newobj, fac));
                foreach (var eq in loadout.Items.Where(x => !string.IsNullOrEmpty(x.Hardpoint)))
                {
                    var equip = Game.GameData.Equipment.Get(eq.EquipCRC);
                    if (equip == null) continue;
                    EquipmentObjectManager.InstantiateEquipment(newobj, Game.ResourceManager, Game.Sound, EquipmentType.LocalPlayer, eq.Hardpoint, equip);
                }
                newobj.Register(gp.world.Physics);
                newobj.AddComponent(new WeaponControlComponent(newobj));
                gp.world.AddObject(newobj);
            });
        }

        private double totalTimeForTick = 0;

        void IClientPlayer.SpawnPlayer(int ID, string system, NetObjective objective, Vector3 position, Quaternion orientation, uint tick)
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
        }

        void IClientPlayer.UpdateAnimations(ObjNetId id, NetCmpAnimation[] animations)
        {
            RunSync(() => gp.world.GetObject(id)?.AnimationComponent?.UpdateAnimations(animations));
        }

        void IClientPlayer.SpawnDebris(int id, GameObjectKind kind, string archetype, string part, Vector3 position, Quaternion orientation, float mass)
        {
            RunSync(() =>
            {
                RigidModel mdl;
                if (kind == GameObjectKind.Ship)
                {
                    var ship = Game.GameData.Ships.Get(archetype);
                    mdl = ((IRigidModelFile) ship.ModelFile.LoadFile(Game.ResourceManager)).CreateRigidModel(true, Game.ResourceManager);
                }
                else
                {
                    var arch = Game.GameData.GetSolarArchetype(archetype);
                    mdl = ((IRigidModelFile) arch.ModelFile.LoadFile(Game.ResourceManager)).CreateRigidModel(true, Game.ResourceManager);
                }
                var newpart = mdl.Parts[part].Clone();
                var newmodel = new RigidModel()
                {
                    Root = newpart,
                    AllParts = new[] { newpart },
                    MaterialAnims = mdl.MaterialAnims,
                    Path = mdl.Path,
                };
                var go = new GameObject($"debris{id}", newmodel, Game.ResourceManager, part, mass, true);
                go.SetLocalTransform(Matrix4x4.CreateFromQuaternion(orientation) *
                                     Matrix4x4.CreateTranslation(position));
                go.Kind = GameObjectKind.Debris;
                go.World = gp.world;
                go.Register(go.World.Physics);
                go.NetID = id;
                go.PhysicsComponent.Body.SetDamping(0.5f, 0.2f);
                gp.world.AddObject(go);
            });
        }

        public SoldGood[] Goods;
        public NetSoldShip[] Ships;

        void IClientPlayer.BaseEnter(string _base, NetObjective objective, string[] rtcs, NewsArticle[] news, SoldGood[] goods, NetSoldShip[] ships)
        {
            if (enterCount > 0 && (connection is EmbeddedServer es)) {
                var path = Game.GetSaveFolder();
                Directory.CreateDirectory(path);
                es.Save(Path.Combine(path, "AutoSave.fl"), null, true);
            }
            CurrentObjective = objective;
            enterCount++;
            PlayerBase = _base;
            News = news;
            Goods = goods;
            Ships = ships;
            SceneChangeRequired();
            AddRTC(rtcs);
        }

        public Dictionary<uint, ulong> BaselinePrices = new Dictionary<uint, ulong>();
        void IClientPlayer.UpdateBaselinePrices(BaselinePrice[] prices)
        {
            foreach (var p in prices)
                BaselinePrices[p.GoodCRC] = p.Price;
        }

        void IClientPlayer.UpdateRTCs(string[] rtcs)
        {
            AddRTC(rtcs);
        }

        void IClientPlayer.DespawnObject(int id, bool explode)
        {
            RunSync(() =>
            {
                var despawn = gp.world.GetNetObject(id);
                if (despawn != null)
                {
                    if (explode)
                        gp.Explode(despawn);
                    despawn.Unregister(gp.world.Physics);
                    gp.world.RemoveObject(despawn);
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

        void IClientPlayer.DestroyPart(ObjNetId id, string part)
        {
            RunSync(() => { gp.world.GetObject(id)?.DisableCmpPart(part); });
        }
        void IClientPlayer.RunMissionDialog(NetDlgLine[] lines)
        {
            RunSync(() => { RunDialog(lines); });
        }

        GameObject missionWaypoint;

        void IClientPlayer.SpawnSolar(SolarInfo[] solars)
        {
            RunSync(() =>
            {
                foreach (var si in solars)
                {
                    var o = gp.world.GetNetObject(si.ID);
                    if (o == null)
                    {
                        var arch = Game.GameData.GetSolarArchetype(si.Archetype);
                        var go = new GameObject(arch, Game.ResourceManager, true);
                        go.SetLocalTransform(Matrix4x4.CreateFromQuaternion(si.Orientation) *
                                             Matrix4x4.CreateTranslation(si.Position));
                        go.Nickname = si.Nickname;
                        go.Name = si.Name;
                        go.World = gp.world;
                        go.Register(go.World.Physics);
                        go.CollisionGroups = arch.CollisionGroups;
                        FLLog.Debug("Client", $"Spawning object {si.ID}");
                        go.NetID = si.ID;
                        if (si.Dock != null && arch.DockSpheres.Count > 0){
                            go.AddComponent(new CDockComponent(go)
                            {
                                Action = si.Dock,
                                DockAnimation = arch.DockSpheres[0].Script,
                                DockHardpoint = arch.DockSpheres[0].Hardpoint,
                                TriggerRadius = arch.DockSpheres[0].Radius
                            });
                        }
                        gp.world.AddObject(go);
                    }
                }
            });
        }

        void IClientPlayer.StopShip() =>
            RunSync(() => gp.StopShip());

        void IClientPlayer.MarkImportant(int id)
        {
            RunSync(() =>
            {
                var o = gp.world.GetNetObject(id);
                if(o == null)
                    FLLog.Warning("Client", $"Could not find obj {id} to mark as important");
                else
                    o.Flags |= GameObjectFlags.Important;
            });
        }


        void IClientPlayer.PlayMusic(string music, float fade) => audioActions.Enqueue(() =>
        {
            if(string.IsNullOrWhiteSpace(music) ||
               music.Equals("none", StringComparison.OrdinalIgnoreCase))
                Game.Sound.StopMusic(fade);
            else
            {
                gp.RtcMusic = true;
                Game.Sound.PlayMusic(music, fade);
            }
        });

        void RunDialog(NetDlgLine[] lines, int index = 0)
        {
            if (index >= lines.Length) return;
            Game.Sound.PlayVoiceLine(lines[index].Voice, lines[index].Hash, () =>
            {
                rpcServer.LineSpoken(lines[index].Hash);
                RunDialog(lines, index + 1);
            });
        }



        void UpdatePackets()
        {
            IPacket packet;
            while (connection.PollPacket(out packet))
            {
                HandlePacket(packet);
            }
        }

        void IClientPlayer.UpdateEffects(ObjNetId id, SpawnedEffect[] effect)
        {
            RunSync(() =>
            {
                var obj = gp.world.GetObject(id);
                if (obj != null)
                {
                    if (!obj.TryGetComponent<CNetEffectsComponent>(out var fx))
                    {
                        fx = new CNetEffectsComponent(obj);
                        obj.AddComponent(fx);
                    }
                    fx.UpdateEffects(effect);
                }
            });
        }

        public void SetDebug(bool on)
        {
            if (connection is EmbeddedServer es)
                es.Server.SendDebugInfo = on;
        }

        public string GetSelectedDebugInfo()
        {
            if (connection is EmbeddedServer es)
                return es.Server.DebugInfo;
            return null;
        }

        public MissionRuntime.TriggerInfo[] GetTriggerInfo()
        {
            if (connection is EmbeddedServer es)
            {
                return es.Server.LocalPlayer?.MissionRuntime?.ActiveTriggersInfo;
            }
            return null;
        }

        void IClientPlayer.CallThorn(string thorn, ObjNetId mainObject)
        {
            RunSync(() =>
            {
                if (thorn == null)
                {
                    gp.Thn = null;
                }
                else
                {
                    var thn = new ThnScript(Game.GameData.ResolveDataPath(thorn));
                    var mo = gp.world.GetObject(mainObject);
                    if (mo != null) FLLog.Info("Client", "Found thorn mainObject");
                    else FLLog.Info("Client", $"Did not find mainObject with ID `{mainObject}`");
                    gp.Thn = new Cutscene(new ThnScriptContext(null) {MainObject = mo}, gp);
                    gp.Thn.BeginScene(thn);
                }
            });
        }


        public NetResponseHandler ResponseHandler;
        public void HandlePacket(IPacket pkt)
        {
            if (ResponseHandler.HandlePacket(pkt))
                return;
            var hcp = GeneratedProtocol.HandleIClientPlayer(pkt, this, connection);
            hcp.Wait();
            if (hcp.Result)
                return;
            if(pkt is not SPUpdatePacket && pkt is not PackedUpdatePacket)
                FLLog.Debug("Client", "Got packet of type " + pkt.GetType());
            switch(pkt)
            {
                case SPUpdatePacket:
                case PackedUpdatePacket:
                    if (processUpdatePackets)
                    {
                        updatePackets.Enqueue(pkt);
                    }
                    break;
                default:
                    if (ExtraPackets != null) ExtraPackets(pkt);
                    else FLLog.Error("Network", "Unknown packet type " + pkt.GetType().ToString());
                    break;
            }
        }

        void UpdateObject(ObjectUpdate update, GameWorld world)
        {
            GameObject obj = world.GetObject(new ObjNetId(update.IsCRC, update.ID));
            if (obj == null)
                return;
            //Component only present in multiplayer
            if (obj.TryGetComponent<CEngineComponent>(out var eng))
            {
                eng.Speed = update.Throttle;
                foreach (var comp in obj.GetChildComponents<CThrusterComponent>())
                    comp.Enabled = (update.CruiseThrust == CruiseThrustState.Thrusting);
            }
            if (obj.TryGetComponent<CHealthComponent>(out var health))
            {
                health.CurrentHealth = update.HullValue;
            }
            var sh = obj.GetFirstChildComponent<CShieldComponent>();
            if (sh != null) {
                sh.SetShieldHealth(update.ShieldValue);
            }
            if (obj.TryGetComponent<WeaponControlComponent>(out var weapons) && (update.Guns?.Length ?? 0) > 0)
            {
                weapons.SetRotations(update.Guns);
            }
            if (obj.SystemObject == null)
            {
                var oldPos = Vector3.Transform(Vector3.Zero, obj.LocalTransform);
                var oldQuat = obj.LocalTransform.ExtractRotation();
                obj.PhysicsComponent.Body.LinearVelocity = update.LinearVelocity.Vector;
                obj.PhysicsComponent.Body.AngularVelocity = update.AngularVelocity.Vector;
                obj.PhysicsComponent.Body.Activate();
                obj.PhysicsComponent.Body.SetTransform(Matrix4x4.CreateFromQuaternion(update.Orientation.Quaternion) * Matrix4x4.CreateTranslation(update.Position));
                SmoothError(obj, oldPos, oldQuat);
            }

            obj.Flags &= ~(GameObjectFlags.Reputations);
            switch (update.RepToPlayer) {
                case RepAttitude.Friendly:
                    obj.Flags |= GameObjectFlags.Friendly;
                    break;
                case RepAttitude.Hostile:
                    obj.Flags |= GameObjectFlags.Hostile;
                    break;
                case RepAttitude.Neutral:
                    obj.Flags |= GameObjectFlags.Neutral;
                    break;
            }
        }

        public void Launch() => rpcServer.Launch();

        void AppendBlue(string text)
        {
            Chats.Append(null, BinaryChatMessage.PlainText(text), Color4.CornflowerBlue, "Arial");
        }

        public void OnChat(ChatCategory category, string str)
        {
            if (str.TrimEnd() == "/ping")
            {
                if (connection is GameNetClient nc)
                {
                    string stats = $"Ping: {nc.Ping}, Loss {nc.LossPercent}%";
                    AppendBlue(stats);
                    AppendBlue($"Sent: {DebugDrawing.SizeSuffix(nc.BytesSent)}, Received: {DebugDrawing.SizeSuffix(nc.BytesReceived)}");
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
                if (gp != null)
                    ((IClientPlayer) this).OnConsoleMessage(Vector3.Transform(Vector3.Zero, gp.player.LocalTransform)
                        .ToString());
                else
                    ((IClientPlayer) this).OnConsoleMessage("null");
            }
            else
            {
                BinaryChatMessage msg;
                if (str[0] == '/' ||
                    !Admin) {
                    msg = BinaryChatMessage.PlainText(str);
                }
                else {
                    msg = BinaryChatMessage.ParseBbCode(str);
                }
                rpcServer.ChatMessage(category, msg);
            }
        }


        void IClientPlayer.ListPlayers(bool isAdmin) =>
            Admin = isAdmin;

        void IClientPlayer.ReceiveChatMessage(ChatCategory category, BinaryChatMessage player, BinaryChatMessage message)
        {
            Chats.Append(player, message, category.GetColor(), "Arial");
        }

        private static int NEW_PLAYER = 393298;
        private static int DEPARTING_PLAYER = 393299;

        private string newPlayerStr;
        private string departingPlayerStr;

        void IClientPlayer.OnPlayerJoin(int id, string name)
        {
            if (newPlayerStr == null)
                newPlayerStr = Game.GameData.GetInfocardText(NEW_PLAYER, Game.Fonts).TrimEnd('\n');
            Chats.Append(null, BinaryChatMessage.PlainText($"{newPlayerStr}{name}"), Color4.DarkRed, "Arial");
        }

        void IClientPlayer.OnPlayerLeave(int id, string name)
        {
            if (departingPlayerStr == null)
                departingPlayerStr = Game.GameData.GetInfocardText(DEPARTING_PLAYER, Game.Fonts).TrimEnd('\n');
            Chats.Append(null, BinaryChatMessage.PlainText($"{departingPlayerStr}{name}"), Color4.DarkRed, "Arial");
        }

        void IClientPlayer.TradelaneActivate(uint id, bool left)
        {
            gameplayActions.Enqueue(() =>
            {
                if (gp.world.GetObject(id)?.TryGetComponent<CTradelaneComponent>(out var tl) ?? false)
                {
                    if(left) tl.ActivateLeft();
                    else tl.ActivateRight();
                }
            });
        }

        void IClientPlayer.TradelaneDeactivate(uint id, bool left)
        {
            gameplayActions.Enqueue(() =>
            {
                if (gp.world.GetObject(id)?.TryGetComponent<CTradelaneComponent>(out var tl) ?? false)
                {
                    if (left) tl.DeactivateLeft();
                    else tl.DeactivateRight();
                }
            });
        }

        GameObject ObjOrPlayer(int id)
        {
            if (id == 0) return gp.player;
            return gp.world.GetNetObject(id);
        }

        void IClientPlayer.UpdateFormation(NetFormation form)
        {
            gameplayActions.Enqueue(() =>
            {
                if (!form.Exists)
                {
                    FLLog.Debug("Client", "Formation null");
                    gp.player.Formation = null;
                }
                else
                {
                    FLLog.Debug("Client", "Formation received");
                    gp.player.Formation = new ShipFormation(
                        ObjOrPlayer(form.LeadShip),
                        form.Followers.Select(ObjOrPlayer).ToArray()
                    );
                    if (gp.player.Formation.LeadShip != gp.player) {
                        FLLog.Debug("Client", "Starting follow");
                        gp.pilotcomponent.StartFormation();
                    }
                }
            });
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
