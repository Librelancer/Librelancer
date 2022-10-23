// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Concurrent;
using System.Numerics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LibreLancer.Data.Missions;
using LibreLancer.GameData.Items;
using LibreLancer.Interface;
using LibreLancer.Net;

namespace LibreLancer
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


    public class CGameSession : IClientPlayer,  INetResponder
	{
        public long Credits;
        public ulong ShipWorth;
		public string PlayerShip;
		public List<string> PlayerComponents = new List<string>();
        public List<NetCargo> Items = new List<NetCargo>();
        public List<StoryCutsceneIni> ActiveCutscenes = new List<StoryCutsceneIni>();
		public FreelancerGame Game;
		public string PlayerSystem;
        public ReputationCollection PlayerReputations = new ReputationCollection();
        public string PlayerBase;
		public Vector3 PlayerPosition;
		public Matrix4x4 PlayerOrientation;
        public NewsArticle[] News = new NewsArticle[0];
        public ChatSource Chats = new ChatSource();
        private IPacketConnection connection;
        private IServerPlayer rpcServer;
        public int CurrentObjectiveIds;
        public IServerPlayer RpcServer => rpcServer;

        private double timeOffset;
        public double WorldTime => Game.TotalTime - timeOffset;

        public bool Multiplayer => connection is GameNetClient;
        private bool paused = false;

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

        private const string SAVE_ALPHABET = "01234567890abcdefghijklmnopqrstuvwxyz";
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
            rpcServer = new RemoteServerPlayer(connection, this);
            ResponseHandler = new NetResponseHandler();
            _updateTick = (int)g.CurrentTick;
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
            objects = new Dictionary<int, GameObject>();
            
            if (PlayerBase != null)
            {
                Game.ChangeState(new RoomGameplay(Game, this, PlayerBase));
                hasChanged = true;
            }
            else
            {
                processUpdatePackets = false;
                gp = new SpaceGameplay(Game, this);
                Game.ChangeState(gp);
                hasChanged = true;
            }
        }

        SpaceGameplay gp;
        Dictionary<int, GameObject> objects = new Dictionary<int, GameObject>();
        
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
        struct PlayerMoveState
        {
            public int Tick;
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
                Sequence =  moveState[^i].Tick,
                Steering = moveState[^i].Steering,
                Strafe = moveState[^i].Strafe,
                Throttle = moveState[^i].Throttle,
                Cruise = moveState[^i].CruiseEnabled,
                Thrust = moveState[^i].Thrust
            };
        }

        private int _updateTick = 0;
        public void GameplayUpdate(SpaceGameplay gp, double delta)
        {
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
                    Tick = _updateTick++,
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
                var ip = new InputUpdatePacket() {Current = FromMoveState(0)};
                if (moveState.Count > 1) ip.HistoryA = FromMoveState(1);
                if (moveState.Count > 2) ip.HistoryB = FromMoveState(2);
                if (moveState.Count > 3) ip.HistoryC = FromMoveState(3);
                connection.SendPacket(ip, PacketDeliveryMethod.SequenceA);

                if (processUpdatePackets)
                {
                    List<ObjectUpdatePacket> toUpdate = new List<ObjectUpdatePacket>();
                    while (updatePackets.Count > 0 && (WorldTime * 1000.0) >= updatePackets.Peek().Tick)
                    {
                        toUpdate.Add(updatePackets.Dequeue());
                    }
                    //Only do resync on the last packet processed this frame
                    //Stops the resync spiral of death
                    for (int i = 0; i < toUpdate.Count; i++) {
                        ProcessUpdate(toUpdate[i], gp, i == toUpdate.Count - 1);
                    }
                }
            }
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
        
        private Queue<ObjectUpdatePacket> updatePackets = new Queue<ObjectUpdatePacket>();

       

        void Resimulate(int i, SpaceGameplay gp)
        {
            var physComponent = gp.player.GetComponent<ShipPhysicsComponent>();
            var player = gp.player;
            physComponent.Tick = moveState[i].Tick;
            physComponent.CurrentStrafe = moveState[i].Strafe;
            physComponent.EnginePower = moveState[i].Throttle;
            physComponent.Steering = moveState[i].Steering;
            physComponent.ThrustEnabled = moveState[i].Thrust;
            physComponent.Update(1 / 60.0f);
            gp.player.World.Physics.StepSimulation(1 / 60.0f);
            moveState[i].Position = player.PhysicsComponent.Body.Position;
            moveState[i].Orientation = player.PhysicsComponent.Body.Transform.ExtractRotation();
        }

        struct SavedObject
        {
            public Matrix4x4 Transform;
            public Vector3 LinearVelocity;
            public Vector3 AngularVelocity;
        }

        void SmoothError(GameObject obj, Vector3 oldPos, Quaternion oldQuat)
        {
            var newPos = obj.PhysicsComponent.Body.Position;
            var newOrient = obj.PhysicsComponent.Body.Transform.ExtractRotation();
            if ((oldPos - newPos).Length() > 10) {
                obj.PhysicsComponent.PredictionErrorPos = Vector3.Zero;
                obj.PhysicsComponent.PredictionErrorQuat = Quaternion.Identity;
            } 
            else {
                obj.PhysicsComponent.PredictionErrorPos = (oldPos - newPos);
                obj.PhysicsComponent.PredictionErrorQuat =
                    Quaternion.Inverse(newOrient) * oldQuat;
            }
        }
        
        void ProcessUpdate(ObjectUpdatePacket p, SpaceGameplay gp, bool resync)
        { 
            foreach (var update in p.Updates)
                UpdateObject(update, gp.world);
            var hp = gp.player.GetComponent<CHealthComponent>();
            var state = p.PlayerState;
            if (hp != null)
            {
                hp.CurrentHealth = state.Health;
                var sh = gp.player.GetChildComponents<CShieldComponent>().FirstOrDefault();
                sh?.SetShieldPercent(state.Shield);
            }
            if(gp?.player != null && resync)
            {
                for (int i = moveState.Count - 1; i >= 0; i--)
                {
                    if (moveState[i].Tick == p.InputSequence)
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
                            //Resimulating messes up the physics world, save state
                            Dictionary<int, SavedObject> savedTransforms = new Dictionary<int, SavedObject>();
                            foreach (var o in objects)
                            {
                                savedTransforms[o.Key] = new SavedObject()
                                {
                                    Transform = o.Value.LocalTransform,
                                    LinearVelocity = o.Value.PhysicsComponent.Body.LinearVelocity,
                                    AngularVelocity = o.Value.PhysicsComponent.Body.AngularVelocity
                                };
                            }

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
                            //simulate inputs
                            for (i = i + 1; i < moveState.Count; i++) {
                                Resimulate(i, gp);
                            }
                            SmoothError(gp.player, predictedPos, predictedOrient);
                            gp.player.PhysicsComponent.Update(1 / 60.0);
                            //Resimulating messes up the physics world, restore state
                            foreach (var o in objects)
                            {
                                var saved = savedTransforms[o.Key];
                                o.Value.SetLocalTransform(saved.Transform);
                                o.Value.PhysicsComponent.Body.LinearVelocity = saved.LinearVelocity;
                                o.Value.PhysicsComponent.Body.AngularVelocity = saved.AngularVelocity;
                            }
                        }

                        break;
                        
                    }
                }
            }
           
        }

        public Action<IPacket> ExtraPackets;
        

        void SetSelfLoadout(NetShipLoadout ld)
        {
            var sh = ld.ShipCRC == 0 ? null : Game.GameData.GetShip((int)ld.ShipCRC);
            PlayerShip = sh?.Nickname ?? null;
            
            Items = new List<NetCargo>(ld.Items.Count);
            if (sh != null)
            {
                foreach (var cg in ld.Items)
                {
                    var equip = Game.GameData.GetEquipment(cg.EquipCRC);
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

        void IClientPlayer.StartTradelane()
        {
            RunSync(() => gp.StartTradelane());
        }

        void IClientPlayer.EndTradelane()
        {
            RunSync(() => gp.EndTradelane());
        }
        

        void IClientPlayer.SpawnProjectiles(ProjectileSpawn[] projectiles)
        {
            RunSync(() =>
            {
                foreach (var p in projectiles)
                {
                    var x = Game.GameData.GetEquipment(p.Gun) as GunEquipment;
                    var projdata = gp.world.Projectiles.GetData(x);
                    gp.world.Projectiles.SpawnProjectile(objects[p.Owner], p.Hardpoint, projdata, p.Start, p.Heading);
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

        void IClientPlayer.ObjectiveUpdate(int objective)
        {
            CurrentObjectiveIds = objective;
            ObjectiveUpdated?.Invoke();
        }

        void IClientPlayer.Killed()
        {
            RunSync(() =>
            {
                gp.Killed();
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
                    if (packet is ClientPacket_BaseEnter || packet is ClientPacket_SpawnPlayer)
                        started = true;
                }
            }
        }

        private int enterCount = 0;

        void IClientPlayer.OnConsoleMessage(string text)
        {
            Chats.Append(text, "Arial", 26, Color4.LimeGreen);
        }

        void RunSync(Action gp) => gameplayActions.Enqueue(gp);

        public Action OnUpdateInventory;
        public Action OnUpdatePlayerShip;

        void IClientPlayer.UpdateReputations(NetReputation[] reps)
        {
            foreach (var r in reps)
            {
                var f = Game.GameData.GetFaction(r.FactionHash);
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

        public void EnqueueAction(Action a) => uiActions.Enqueue(a);

        void IClientPlayer.SpawnObject(int id, ObjectName name, string affiliation, Vector3 position, Quaternion orientation, NetShipLoadout loadout)
        {
            RunSync(() =>
            {
                var shp = Game.GameData.GetShip((int) loadout.ShipCRC);
                var newobj = new GameObject(shp, Game.ResourceManager, true, true) {
                    World = gp.world
                };
                newobj.Name = name;
                newobj.NetID = id;
                newobj.SetLocalTransform(Matrix4x4.CreateFromQuaternion(orientation) *
                                         Matrix4x4.CreateTranslation(position));
                newobj.Components.Add(new CHealthComponent(newobj) { CurrentHealth = loadout.Health, MaxHealth = shp.Hitpoints });
                newobj.Components.Add(new CDamageFuseComponent(newobj, shp.Fuses));
                var fac = Game.GameData.GetFaction(affiliation);
                if(fac != null)
                    newobj.Components.Add(new CFactionComponent(newobj, fac));
                foreach (var eq in loadout.Items.Where(x => !string.IsNullOrEmpty(x.Hardpoint)))
                {
                    var equip = Game.GameData.GetEquipment(eq.EquipCRC);
                    if (equip == null) continue;
                    EquipmentObjectManager.InstantiateEquipment(newobj, Game.ResourceManager, Game.Sound, EquipmentType.LocalPlayer, eq.Hardpoint, equip);
                }
                newobj.Register(gp.world.Physics);

                newobj.Components.Add(new WeaponControlComponent(newobj));
                objects.Add(id, newobj);
                
                gp.world.AddObject(newobj);
            });
        }

        void IClientPlayer.SpawnPlayer(string system, int objective, Vector3 position, Quaternion orientation)
        {
            enterCount++;
            PlayerBase = null;
            CurrentObjectiveIds = objective;
            FLLog.Info("Client", $"Spawning in {system}");
            PlayerSystem = system;
            PlayerPosition = position;
            PlayerOrientation = Matrix4x4.CreateFromQuaternion(orientation);
            SceneChangeRequired();
        }

        void IClientPlayer.StartAnimation(bool systemObject, int id, string anim)
        {
            RunSync(() =>
            {
                GameObject obj;
                if (systemObject)
                    obj = gp.world.GetObject((uint) id);
                else
                    obj = objects[id];
                obj?.AnimationComponent?.StartAnimation(anim, false);
            });
        }

        void IClientPlayer.SpawnDebris(int id, GameObjectKind kind, string archetype, string part, Vector3 position, Quaternion orientation, float mass)
        {
            RunSync(() =>
            {
                RigidModel mdl;
                if (kind == GameObjectKind.Ship)
                {
                    var ship = Game.GameData.GetShip(archetype);
                    mdl = ((IRigidModelFile) ship.ModelFile.LoadFile(Game.ResourceManager)).CreateRigidModel(true);
                }
                else
                {
                    var arch = Game.GameData.GetSolarArchetype(archetype);
                    mdl = ((IRigidModelFile) arch.ModelFile.LoadFile(Game.ResourceManager)).CreateRigidModel(true);
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
                if (go.PhysicsComponent != null) go.PhysicsComponent.SetTransform = false;
                go.World = gp.world;
                go.Register(go.World.Physics);
                go.NetID = id;
                gp.world.AddObject(go);
                objects.Add(id, go);
            });
        }

        public SoldGood[] Goods;
        public NetSoldShip[] Ships;

        void IClientPlayer.BaseEnter(string _base, int objective, string[] rtcs, NewsArticle[] news, SoldGood[] goods, NetSoldShip[] ships)
        {
            if (enterCount > 0 && (connection is EmbeddedServer es)) {
                var path = Game.GetSaveFolder();
                Directory.CreateDirectory(path);
                es.Save(Path.Combine(path, "AutoSave.fl"), null, true);
                Game.Saves.UpdateFile(Path.Combine(path, "AutoSave.fl"));
            }
            CurrentObjectiveIds = objective;
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

        void IClientPlayer.DespawnObject(int id)
        {
            RunSync(() =>
            {
                if (objects.TryGetValue(id, out var despawn))
                {
                    despawn.Unregister(gp.world.Physics);
                    gp.world.RemoveObject(despawn);
                    objects.Remove(id);
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

        void IClientPlayer.DestroyPart(byte idtype, int id, string part)
        {
            RunSync(() => { objects[id].DisableCmpPart(part); });
        }
        void IClientPlayer.RunMissionDialog(NetDlgLine[] lines)
        {
            RunSync(() => { RunDialog(lines); });
        }

        void IClientPlayer.SpawnSolar(SolarInfo[] solars)
        {
            RunSync(() =>
            {
                foreach (var si in solars)
                {
                    if (!objects.ContainsKey(si.ID))
                    {
                        var arch = Game.GameData.GetSolarArchetype(si.Archetype);
                        var go = new GameObject(arch, Game.ResourceManager, true);
                        go.SetLocalTransform(Matrix4x4.CreateFromQuaternion(si.Orientation) *
                                             Matrix4x4.CreateTranslation(si.Position));
                        if (go.PhysicsComponent != null) go.PhysicsComponent.SetTransform = false;
                        go.Nickname = $"$Solar{si.ID}";
                        go.World = gp.world;
                        go.Register(go.World.Physics);
                        go.CollisionGroups = arch.CollisionGroups;
                        FLLog.Debug("Client", $"Spawning object {si.ID}");
                        go.NetID = si.ID;
                        gp.world.AddObject(go);
                        objects.Add(si.ID, go);
                    }
                }
            });
        }
        
        void IClientPlayer.PlayMusic(string music) => audioActions.Enqueue(() =>
        {
            if(string.IsNullOrWhiteSpace(music) ||
               music.Equals("none", StringComparison.OrdinalIgnoreCase))
                Game.Sound.StopMusic();
            else
                Game.Sound.PlayMusic(music);
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

        void IClientPlayer.UpdateEffects(int id, SpawnedEffect[] effect)
        {
            RunSync(() =>
            {
                if (objects.TryGetValue(id, out var obj))
                {
                    if (!obj.TryGetComponent<CNetEffectsComponent>(out var fx))
                    {
                        fx = new CNetEffectsComponent(obj);
                        obj.Components.Add(fx);
                    }

                    fx.UpdateEffects(effect);
                }
            });
        }

        public MissionRuntime.TriggerInfo[] GetTriggerInfo()
        {
            if (connection is EmbeddedServer es)
            {
                return es.Server.LocalPlayer?.MissionRuntime?.ActiveTriggersInfo;
            }
            return null;
        }

        void IClientPlayer.CallThorn(string thorn, int mainObject)
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
                    objects.TryGetValue(mainObject, out var mo);
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
            var hcp = GeneratedProtocol.HandleClientPacket(pkt, this, this);
            hcp.Wait();
            if (hcp.Result)
                return;
            if(!(pkt is ObjectUpdatePacket))
                FLLog.Debug("Client", "Got packet of type " + pkt.GetType());
            switch(pkt)
            {
                case ObjectUpdatePacket p:
                    if (processUpdatePackets)
                    {
                        updatePackets.Enqueue(p);
                    }
                    else timeOffset = Game.TotalTime - (p.Tick / 1000.0);
                    break;
                default:
                    if (ExtraPackets != null) ExtraPackets(pkt);
                    else FLLog.Error("Network", "Unknown packet type " + pkt.GetType().ToString());
                    break;
            }
        }

        void UpdateObject(PackedShipUpdate update, GameWorld world)
        {
            GameObject obj;
            if (update.IsCRC) {
                obj = world.GetObject((uint) update.ID);
            }
            else {
                if (!objects.TryGetValue(update.ID, out obj))
                    return;
            }
            //Component only present in multiplayer
            if (update.HasPosition && obj.TryGetComponent<CEngineComponent>(out var eng))
            {
                eng.Speed = update.Throttle;
                foreach (var comp in obj.GetChildComponents<CThrusterComponent>())
                    comp.Enabled = (update.CruiseThrust == CruiseThrustState.Thrusting);
            }
            if (obj.TryGetComponent<CHealthComponent>(out var health))
            {
                if (update.Hull)
                    health.CurrentHealth = update.HullValue;
                else
                    health.CurrentHealth = health.MaxHealth;
            }
            var sh = obj.GetChildComponents<CShieldComponent>().FirstOrDefault();
            if (sh != null) {
                if (update.Shield == 0) sh.SetShieldPercent(0);
                else if (update.Shield == 1) sh.SetShieldPercent(1);
                else sh.SetShieldPercent(update.ShieldValue);
            }
            if (obj.TryGetComponent<WeaponControlComponent>(out var weapons) && (update.Guns?.Length ?? 0) > 0)
            {
                weapons.SetRotations(update.Guns);
            }
            if (update.HasPosition)
            {
                var oldPos = Vector3.Transform(Vector3.Zero, obj.LocalTransform);
                var oldQuat = obj.LocalTransform.ExtractRotation();
                obj.PhysicsComponent.Body.LinearVelocity = update.LinearVelocity;
                obj.PhysicsComponent.Body.AngularVelocity = update.AngularVelocity;
                obj.PhysicsComponent.Body.Activate();
                obj.PhysicsComponent.Body.SetTransform(Matrix4x4.CreateFromQuaternion(update.Orientation) * Matrix4x4.CreateTranslation(update.Position));
                SmoothError(obj, oldPos, oldQuat);
            }
        }

        public void Launch() => rpcServer.Launch();

        public void OnChat(ChatCategory category, string str)
        {
            if (str.TrimEnd() == "/netstat")
            {
                if (connection is GameNetClient nc)
                {
                    string stats = $"Ping: {nc.Ping}, Loss {nc.LossPercent}%";
                    Chats.Append(stats, "Arial", 26, Color4.CornflowerBlue);
                    Chats.Append(
                        $"Sent: {DebugDrawing.SizeSuffix(nc.BytesSent)}, Received: {DebugDrawing.SizeSuffix(nc.BytesReceived)}",
                        "Arial", 26, Color4.CornflowerBlue);
                }
                else
                {
                    Chats.Append("Offline", "Arial", 26, Color4.CornflowerBlue);
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
            else {
                rpcServer.ChatMessage(category, str);  
            }
        }

        

        void IClientPlayer.ReceiveChatMessage(ChatCategory category, string player, string message)
        {
            Chats.Append($"{player}: {message}", "Arial", 26, category.GetColor());
        }
        
        private static int NEW_PLAYER = 393298;
        private static int DEPARTING_PLAYER = 393299;

        private string newPlayerStr;
        private string departingPlayerStr;

        void IClientPlayer.OnPlayerJoin(int id, string name)
        {
            if (newPlayerStr == null)
                newPlayerStr = Game.GameData.GetInfocardText(NEW_PLAYER, Game.Fonts).TrimEnd('\n');
            Chats.Append($"{newPlayerStr}{name}", "Arial", 26, Color4.DarkRed);
        }

        void IClientPlayer.OnPlayerLeave(int id, string name)
        {
            if (departingPlayerStr == null)
                departingPlayerStr = Game.GameData.GetInfocardText(DEPARTING_PLAYER, Game.Fonts).TrimEnd('\n');
            Chats.Append($"{departingPlayerStr}{name}", "Arial", 26, Color4.DarkRed);
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
            return objects[id];
        }
        
        void IClientPlayer.UpdateFormation(NetFormation form)
        {
            gameplayActions.Enqueue(() =>
            {
                if (!form.Exists)
                {
                    gp.player.Formation = null;
                }
                else
                {
                    gp.player.Formation = new ShipFormation(
                        ObjOrPlayer(form.LeadShip),
                        form.Followers.Select(ObjOrPlayer).ToArray()
                    );
                    if (gp.player.Formation.LeadShip != gp.player) {
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

        void INetResponder.SendResponse(IPacket packet) => connection.SendPacket(packet, PacketDeliveryMethod.ReliableOrdered);
    }
}
