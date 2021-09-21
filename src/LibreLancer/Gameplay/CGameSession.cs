// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Collections.Generic;
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
            var player = gp.world.GetObject("player");
            var tr = player.WorldTransform;
            var pos = Vector3.Transform(Vector3.Zero, tr);
            var orient = tr.ExtractRotation();
            connection.SendPacket(new PositionUpdatePacket()
            {
                Position =  pos,
                Orientation = orient
            }, PacketDeliveryMethod.SequenceB);
        }

        public void WorldReady()
        {
            while (gameplayActions.TryDequeue(out var act))
                act();
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
                else
                    hp = hpcrcs[eq.HardpointCRC];
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

        void IClientPlayer.FireProjectiles(ProjectileSpawn[] projectiles)
        {
            RunSync(() =>
            {
                foreach (var p in projectiles)
                {
                    var x = Game.GameData.GetEquipment(p.Gun) as GunEquipment;
                    var projdata = gp.world.Projectiles.GetData(x);
                    gp.world.Projectiles.SpawnProjectile(projdata, p.Start, p.Heading);
                }
            });
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

        void IClientPlayer.OnConsoleMessage(string text)
        {
            Chats.Append(text, "Arial", 10, Color4.LimeGreen);
        }

        void RunSync(Action gp) => gameplayActions.Enqueue(gp);

        public Action OnUpdateInventory;

        void IClientPlayer.UpdateInventory(long credits, NetShipLoadout loadout)
        {
            Credits = credits;
            SetSelfLoadout(loadout);
            if (OnUpdateInventory != null) {
                uiActions.Enqueue(OnUpdateInventory);
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
                newobj.Name = "NetPlayer " + id;
                newobj.SetLocalTransform(Matrix4x4.CreateFromQuaternion(orientation) *
                                         Matrix4x4.CreateTranslation(position));
                newobj.Components.Add(new HealthComponent(newobj) { CurrentHealth = loadout.Health, MaxHealth = shp.Hitpoints });
                newobj.Components.Add(new CDamageFuseComponent(newobj, shp.Fuses));
                newobj.Register(gp.world.Physics);
                if(connection is GameNetClient) 
                    newobj.Components.Add(new CNetPositionComponent(newobj));
                objects.Add(id, newobj);
                gp.world.Objects.Add(newobj);
            });
        }

        void IClientPlayer.SpawnPlayer(string system, double systemTime, Vector3 position, Quaternion orientation, long credits, NetShipLoadout ship)
        {
            PlayerBase = null;
            SpawnTime = systemTime;
            FLLog.Info("Client", $"Spawning at time {systemTime}");
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

        void IClientPlayer.SpawnDebris(int id, string archetype, string part, Vector3 position, Quaternion orientation, float mass)
        {
            RunSync(() =>
            {
                var arch = Game.GameData.GetSolarArchetype(archetype);
                var mdl =
                    ((IRigidModelFile) arch.ModelFile.LoadFile(Game.ResourceManager)).CreateRigidModel(true);
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
                gp.world.Objects.Add(go);
                objects.Add(id, go);
            });
        }

        public SoldGood[] Goods;

        void IClientPlayer.BaseEnter(string _base, long credits, NetShipLoadout ship, string[] rtcs, NewsArticle[] news, SoldGood[] goods)
        {
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
            gp.world.Objects.Remove(despawn);
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
                        gp.world.Objects.Add(go);
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

        void IClientPlayer.CallThorn(string thorn)
        {
            RunSync(() =>
            {
                var thn = new ThnScript(Game.GameData.ResolveDataPath(thorn));
                gp.Thn = new Cutscene(new ThnScriptContext(null), gp);
                gp.Thn.BeginScene(thn);
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
                    RunSync(() =>
                    {
                        var hp = gp?.player?.GetComponent<HealthComponent>();
                        if(hp != null)
                            hp.CurrentHealth = p.PlayerHealth;
                        foreach (var update in p.Updates)
                            UpdateObject(p.Tick, update);
                    });
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
            if (netPos != null)
            {
                if(update.HasPosition) netPos.QueuePosition(tick, update.Position);
                if(update.HasOrientation) netPos.QueueOrientation(tick, update.Orientation);
                if (update.HasHealth) {
                    var health = obj.GetComponent<HealthComponent>();
                    if (health != null)
                        health.CurrentHealth = (float)update.HullHp;
                }
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
            else
            {
                rpcServer.ConsoleCommand(str);
            }
        }
        

        public void Disconnected()
        {
            Game.ChangeState(new LuaMenu(Game));
        }

        public void OnExit()
        {
            connection.Shutdown();
        }

        void INetResponder.SendResponse(IPacket packet) => connection.SendPacket(packet, PacketDeliveryMethod.ReliableOrdered);
    }
}
