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
		public string PlayerBase;
		public Vector3 PlayerPosition;
		public Matrix4x4 PlayerOrientation;
        public NewsArticle[] News = new NewsArticle[0];
        public ChatSource Chats = new ChatSource();
        private IPacketConnection connection;
        private IServerPlayer rpcServer;
        public IServerPlayer RpcServer => rpcServer;

        private double timeOffset;
        public double WorldTime => Game.TotalTime - timeOffset;

        public bool Multiplayer => connection is GameNetClient;

        public void Pause()
        {
            (connection as EmbeddedServer)?.Server.LocalPlayer.World?.Pause();
        }

        public void Resume()
        {
            (connection as EmbeddedServer)?.Server.LocalPlayer.World?.Resume();
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
        
        public void GameplayUpdate(SpaceGameplay gp, double delta)
        {
            UpdateAudio();
            while (gameplayActions.TryDequeue(out var act))
                act();
            var player = gp.player;
            var phys = player.GetComponent <ShipPhysicsComponent>();
            connection.SendPacket(new InputUpdatePacket()
            {
                Pitch = Math.Abs(phys.PlayerPitch) > 0 ? phys.PlayerPitch : phys.Pitch,
                Yaw = Math.Abs(phys.PlayerYaw) > 0 ? phys.PlayerYaw : phys.Yaw,
                Roll = phys.Roll,
                Throttle = phys.EnginePower
            }, PacketDeliveryMethod.SequenceA);
            
            if (processUpdatePackets)
            {
                while (updatePackets.Count > 0 && (WorldTime * 1000.0) >= updatePackets.Peek().Tick)
                {
                    ProcessUpdate(updatePackets.Dequeue());
                }
            }
        }

        volatile bool processUpdatePackets = false;
        
        
        public void WorldReady()
        {
            while (gameplayActions.TryDequeue(out var act))
                act();
        }

        public void BeginUpdateProcess()
        {
            processUpdatePackets = true;
        }
        
        private Queue<ObjectUpdatePacket> updatePackets = new Queue<ObjectUpdatePacket>();

        void ProcessUpdate(ObjectUpdatePacket p)
        {
            var hp = gp.player.GetComponent<CHealthComponent>();
            if (hp != null)
            {
                   hp.CurrentHealth = p.PlayerHealth;
                   hp.ShieldHealth = p.PlayerShield;
            }
            if(gp?.player != null)
            {
                gp.player.SetLocalTransform(Matrix4x4.CreateFromQuaternion(p.PlayerRotation) *
                                                Matrix4x4.CreateTranslation(p.PlayerPosition));
                gp.player.PhysicsComponent.Body.LinearVelocity = p.PlayerLinearVelocity;
                gp.player.PhysicsComponent.Body.AngularVelocity = p.PlayerAngularVelocity;
            }
            foreach (var update in p.Updates)
                UpdateObject(update);
        }

        public Action<IPacket> ExtraPackets;
        

        void SetSelfLoadout(NetShipLoadout ld)
        {
            var sh = Game.GameData.GetShip((int)ld.ShipCRC);
            PlayerShip = sh.Nickname;
            var hplookup = new HardpointLookup(sh.ModelFile.LoadFile(Game.ResourceManager));
            Items = new List<NetCargo>(ld.Items.Count);
            foreach (var cg in ld.Items)
            {
                var equip = Game.GameData.GetEquipment(cg.EquipCRC);
                Items.Add(new NetCargo(cg.ID)
                {
                    Equipment = equip,
                    Hardpoint = hplookup.GetHardpoint(cg.HardpointCRC),
                    Health = cg.Health / 255f,
                    Count = cg.Count
                });
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
            Chats.Append(text, "Arial", 10, Color4.LimeGreen);
        }

        void RunSync(Action gp) => gameplayActions.Enqueue(gp);

        public Action OnUpdateInventory;
        public Action OnUpdatePlayerShip;

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

        void IClientPlayer.SpawnObject(int id, string name, Vector3 position, Quaternion orientation, NetShipLoadout loadout)
        {
            RunSync(() =>
            {
                var shp = Game.GameData.GetShip((int) loadout.ShipCRC);
                //Set up player object + camera
                var newobj = new GameObject(shp, Game.ResourceManager, true, true) {
                    World = gp.world
                };
                if (newobj.PhysicsComponent != null) newobj.PhysicsComponent.SetTransform = false;
                newobj.Name = name;
                newobj.SetLocalTransform(Matrix4x4.CreateFromQuaternion(orientation) *
                                         Matrix4x4.CreateTranslation(position));
                newobj.Components.Add(new CHealthComponent(newobj) { CurrentHealth = loadout.Health, MaxHealth = shp.Hitpoints });
                newobj.Components.Add(new CDamageFuseComponent(newobj, shp.Fuses));
                var hplookup = new HardpointLookup(shp.ModelFile.LoadFile(Game.ResourceManager));
                foreach (var eq in loadout.Items.Where(x => x.HardpointCRC != 0))
                {
                    var equip = Game.GameData.GetEquipment(eq.EquipCRC);
                    if (equip == null) continue;
                    EquipmentObjectManager.InstantiateEquipment(newobj, Game.ResourceManager, EquipmentType.LocalPlayer, hplookup.GetHardpoint(eq.HardpointCRC), equip);
                }
                newobj.Register(gp.world.Physics);

                newobj.Components.Add(new WeaponControlComponent(newobj));
                objects.Add(id, newobj);
                
                gp.world.AddObject(newobj);
            });
        }

        void IClientPlayer.SpawnPlayer(string system, double systemTime, Vector3 position, Quaternion orientation)
        {
            enterCount++;
            PlayerBase = null;
            FLLog.Info("Client", $"Spawning in {system} at time {systemTime}");
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
                gp.world.AddObject(go);
                objects.Add(id, go);
            });
        }

        public SoldGood[] Goods;
        public NetSoldShip[] Ships;

        void IClientPlayer.BaseEnter(string _base, string[] rtcs, NewsArticle[] news, SoldGood[] goods, NetSoldShip[] ships)
        {
            if (enterCount > 0 && (connection is EmbeddedServer es)) {
                var path = Game.GetSaveFolder();
                Directory.CreateDirectory(path);
                es.Save(Path.Combine(path, "AutoSave.fl"), null, true);
                Game.Saves.UpdateFile(Path.Combine(path, "AutoSave.fl"));
            }
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

        void IClientPlayer.ForceMove(Vector3 position)
        {
            RunSync(() =>
            {
                PlayerPosition = position;
                var player = gp.player;
                var rot = player.LocalTransform.ExtractRotation();
                player.SetLocalTransform(Matrix4x4.CreateFromQuaternion(rot) * Matrix4x4.CreateTranslation(position));
            });
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
                    if (processUpdatePackets) updatePackets.Enqueue(p);
                    else timeOffset = Game.TotalTime - (p.Tick / 1000.0);
                    break;
                default:
                    if (ExtraPackets != null) ExtraPackets(pkt);
                    else FLLog.Error("Network", "Unknown packet type " + pkt.GetType().ToString());
                    break;
            }
        }

        void UpdateObject(PackedShipUpdate update)
        {
            if (!objects.ContainsKey(update.ID)) return;
            var obj = objects[update.ID];
            //Component only present in multiplayer
            if (update.HasPosition && obj.TryGetComponent<CEngineComponent>(out var eng))
            {
                eng.Speed = update.Throttle;
            }
            if (obj.TryGetComponent<CHealthComponent>(out var health))
            {
                if (update.Hull)
                    health.CurrentHealth = update.HullValue;
                else
                    health.CurrentHealth = health.MaxHealth;
                /*if (obj.TryGetComponent<CShieldComponent>(out var shield))
                {
                    if (update.Shield == 0) shield.ShieldPercent = 0;
                }*/
            }
            if (obj.TryGetComponent<WeaponControlComponent>(out var weapons) && (update.Guns?.Length ?? 0) > 0)
            {
                weapons.SetRotations(update.Guns);
            }

            if (update.HasPosition)
            {
                obj.SetLocalTransform(Matrix4x4.CreateFromQuaternion(update.Orientation) * Matrix4x4.CreateTranslation(update.Position));
            }
        }

        public void Launch() => rpcServer.Launch();

        public void ProcessConsoleCommand(string str)
        {
            Chats.Append(str, "Arial", 9, Color4.Green);
            if (str == "#netstat")
            {
                if(connection is GameNetClient nc)
                {
                    string stats = $"Ping: {nc.Ping}, Loss {nc.LossPercent}%";
                    Chats.Append(stats, "Arial", 9, Color4.CornflowerBlue);
                    Chats.Append($"Sent: {DebugDrawing.SizeSuffix(nc.BytesSent)}, Received: {DebugDrawing.SizeSuffix(nc.BytesReceived)}", "Arial", 9, Color4.CornflowerBlue);
                }
                else
                {
                    Chats.Append("Offline", "Arial", 9, Color4.CornflowerBlue);
                }
            }
            else if (str == "#debug")
            {
                Game.Debug.Enabled = !Game.Debug.Enabled;
            }
            else
            {
                rpcServer.ConsoleCommand(str);
            }
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
