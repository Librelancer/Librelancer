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
using LibreLancer.Utf.Cmp;
using LiteNetLib;

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
		public string PlayerShip;
		public List<string> PlayerComponents = new List<string>();
        public List<EquipMount> Mounts = new List<EquipMount>();
        public List<NetCargo> Cargo = new List<NetCargo>();
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

        public double SpawnTime; //server time at time of spawn
        private double tOffset; //game time at time of spawn
        public double WorldTime => Game.TotalTime - tOffset + SpawnTime;

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
        
        public void GameplayUpdate(SpaceGameplay gp)
        {
            UpdateAudio();
            while (gameplayActions.TryDequeue(out var act))
                act();
            var player = gp.player;
            var tr = player.WorldTransform;
            var pos = Vector3.Transform(Vector3.Zero, tr);
            var orient = tr.ExtractRotation();
            connection.SendPacket(new PositionUpdatePacket()
            {
                Position =  pos,
                Orientation = orient,
                Speed = player.GetComponent<CEngineComponent>()?.Speed ?? 0
            }, PacketDeliveryMethod.SequenceB);
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

        public Action<IPacket> ExtraPackets;

        static IEnumerable<string> HardpointList(IDrawable dr)
        {
            if(dr is ModelFile)
            {
                var mdl = (ModelFile)dr;
                foreach (var hp in mdl.Hardpoints)
                    yield return hp.Name;
            }
            else if (dr is CmpFile)
            {
                var cmp = (CmpFile)dr;
                foreach(var model in cmp.Models.Values)
                {
                    foreach (var hp in model.Hardpoints)
                        yield return hp.Name;
                }
            }
        }

        void SetSelfLoadout(NetShipLoadout ld)
        {
            var sh = Game.GameData.GetShip((int)ld.ShipCRC);
            PlayerShip = sh.Nickname;
            var hpcrcs = new Dictionary<uint, string>();
            foreach (var hp in HardpointList(sh.ModelFile.LoadFile(Game.ResourceManager)))
                hpcrcs.Add(CrcTool.FLModelCrc(hp), hp);
            Mounts = new List<EquipMount>();
            foreach (var eq in ld.Equipment)
            {
                string hp;
                if (eq.HardpointCRC == 0)
                    hp = null;
                else if (!hpcrcs.TryGetValue(eq.HardpointCRC, out hp))
                    continue;
                Mounts.Add(new EquipMount(
                    hp,
                    Game.GameData.GetEquipment(eq.EquipCRC).Nickname
                ));
            }
            Cargo = new List<NetCargo>();
            foreach (var cg in ld.Cargo)
            {
                var equip = Game.GameData.GetEquipment(cg.EquipCRC);
                Cargo.Add(new NetCargo(cg.ID) { Equipment = equip, Count = cg.Count });
            }
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

        void IClientPlayer.UpdateInventory(long credits, NetShipLoadout loadout)
        {
            Credits = credits;
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
                newobj.Name = name;
                newobj.SetLocalTransform(Matrix4x4.CreateFromQuaternion(orientation) *
                                         Matrix4x4.CreateTranslation(position));
                newobj.Components.Add(new HealthComponent(newobj) { CurrentHealth = loadout.Health, MaxHealth = shp.Hitpoints });
                newobj.Components.Add(new CDamageFuseComponent(newobj, shp.Fuses));
                var hpcrcs = new Dictionary<uint, string>();
                foreach (var hp in HardpointList(shp.ModelFile.LoadFile(Game.ResourceManager)))
                    hpcrcs.Add(CrcTool.FLModelCrc(hp), hp);
                Mounts = new List<EquipMount>();
                foreach (var eq in loadout.Equipment)
                {
                    string hp = eq.HardpointCRC == 0 ? null : hpcrcs[eq.HardpointCRC];
                    var equip = Game.GameData.GetEquipment(eq.EquipCRC);
                    if (equip == null) continue;
                    EquipmentObjectManager.InstantiateEquipment(newobj, Game.ResourceManager, EquipmentType.LocalPlayer, hp, equip);
                }
                newobj.Register(gp.world.Physics);
                var netpos = new CNetPositionComponent(newobj);
                if (connection is EmbeddedServer)
                    netpos.BufferTime = 2;
                newobj.Components.Add(netpos);
                newobj.Components.Add(new WeaponControlComponent(newobj));
                objects.Add(id, newobj);
                
                gp.world.AddObject(newobj);
            });
        }

        void IClientPlayer.SpawnPlayer(string system, double systemTime, Vector3 position, Quaternion orientation, long credits, NetShipLoadout ship)
        {
            enterCount++;
            PlayerBase = null;
            SpawnTime = systemTime;
            FLLog.Info("Client", $"Spawning in {system} at time {systemTime}");
            tOffset = Game.TotalTime;
            Credits = credits;
            PlayerSystem = system;
            PlayerPosition = position;
            PlayerOrientation = Matrix4x4.CreateFromQuaternion(orientation);
            SetSelfLoadout(ship);
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
                go.World = gp.world;
                go.Register(go.World.Physics);
                gp.world.AddObject(go);
                objects.Add(id, go);
            });
        }

        public SoldGood[] Goods;

        void IClientPlayer.BaseEnter(string _base, long credits, NetShipLoadout ship, string[] rtcs, NewsArticle[] news, SoldGood[] goods)
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
            Credits = credits;
            SetSelfLoadout(ship);
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
            var despawn = objects[id];
            despawn.Unregister(gp.world.Physics);
            gp.world.RemoveObject(despawn);
            objects.Remove(id);
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
        
        void IClientPlayer.PlayMusic(string music) => audioActions.Enqueue(() => Game.Sound.PlayMusic(music));

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
                var obj = objects[id];
                if (!obj.TryGetComponent<CNetEffectsComponent>(out var fx))
                {
                    fx = new CNetEffectsComponent(obj);
                    obj.Components.Add(fx);
                }
                fx.UpdateEffects(effect);
            });
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
                        RunSync(() =>
                        {
                            var hp = gp?.player?.GetComponent<HealthComponent>();
                            if (hp != null)
                                hp.CurrentHealth = p.PlayerHealth;
                            foreach (var update in p.Updates)
                                UpdateObject(p.Tick, update);
                        });
                    }
                    break;
                default:
                    if (ExtraPackets != null) ExtraPackets(pkt);
                    else FLLog.Error("Network", "Unknown packet type " + pkt.GetType().ToString());
                    break;
            }
        }

        void UpdateObject(uint tick, PackedShipUpdate update)
        {
            if (!objects.ContainsKey(update.ID)) return;
            var obj = objects[update.ID];
            //Component only present in multiplayer
            var netPos = obj.GetComponent<CNetPositionComponent>();
            if (update.HasPosition && obj.TryGetComponent<CEngineComponent>(out var eng)) {
                eng.Speed = update.EngineThrottlePct / 255f;
            }
            if (update.HasHealth) {
                var health = obj.GetComponent<HealthComponent>();
                if (health != null)
                    health.CurrentHealth = (float)update.HullHp;
            }

            if (update.HasGuns && obj.TryGetComponent<WeaponControlComponent>(out var weapons))
            {
                weapons.SetRotations(update.GunOrients);
            }
            if (netPos != null)
            {
                if(update.HasPosition) netPos.QueuePosition(tick, update.Position);
                if(update.HasOrientation) netPos.QueueOrientation(tick, update.Orientation);
            }
            else
            {
                var tr = obj.WorldTransform;
                var pos = update.HasPosition ? update.Position : Vector3.Transform(Vector3.Zero, tr);
                var rot = update.HasOrientation ? update.Orientation : tr.ExtractRotation();
                obj.SetLocalTransform(Matrix4x4.CreateFromQuaternion(rot) * Matrix4x4.CreateTranslation(pos));
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
