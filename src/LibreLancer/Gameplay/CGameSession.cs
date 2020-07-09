// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Collections.Generic;
using LibreLancer.Data.Missions;
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


    public class CGameSession
	{
        public long Credits;
		public string PlayerShip;
		public List<string> PlayerComponents = new List<string>();
        public List<EquipMount> Mounts = new List<EquipMount>();
        public List<StoryCutsceneIni> ActiveCutscenes = new List<StoryCutsceneIni>();
		public FreelancerGame Game;
		public string PlayerSystem;
		public string PlayerBase;
		public Vector3 PlayerPosition;
		public Matrix4x4 PlayerOrientation;

        private IPacketConnection connection;
		public CGameSession(FreelancerGame g, IPacketConnection connection)
		{
			Game = g;
            this.connection = connection;
        }

        public void AddRTC(string[] paths)
        {
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
            connection.SendPacket(new RTCCompletePacket() {RTC = cutscene.RefPath},
                PacketDeliveryMethod.ReliableOrdered);
        }

        public void RoomEntered(string room, string bse)
        {
            connection.SendPacket(new EnterLocationPacket() { Base = bse, Room = room}, PacketDeliveryMethod.ReliableOrdered);
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
            return hasChanged;
        }

        Queue<Action> gameplayActions = new Queue<Action>();
        private Queue<Action> audioActions = new Queue<Action>();

        void UpdateAudio()
        {
            while (audioActions.TryDequeue(out var act))
                act();
        }
        
        public void GameplayUpdate(SpaceGameplay gp)
        {
            UpdateAudio();
            while (gameplayActions.TryDequeue(out var act))
                act();
            var player = gp.world.GetObject("player");
            var tr = player.GetTransform();
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
                    if (packet is BaseEnterPacket || packet is SpawnPlayerPacket)
                        started = true;
                }
            }
        }

        void RunSync(Action gp) => gameplayActions.Enqueue(gp);

        void PlaySound(string sound) => audioActions.Enqueue(() => Game.Sound.PlaySound(sound));
        void PlayMusic(string music) => audioActions.Enqueue(() => Game.Sound.PlayMusic(music));

        void RunDialog(NetDlgLine[] lines, int index = 0)
        {
            if (index >= lines.Length) return;
            Game.Sound.PlayVoiceLine(lines[index].Voice, lines[index].Hash, () =>
            {
                connection.SendPacket(new LineSpokenPacket() { Hash = lines[index].Hash }, PacketDeliveryMethod.ReliableOrdered);
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
        public void HandlePacket(IPacket pkt)
        {
            if(!(pkt is ObjectUpdatePacket))
                FLLog.Debug("Client", "Got packet of type " + pkt.GetType());
            switch(pkt)
            {
                case CallThornPacket ct:
                    RunSync(() => {
                        var thn = new ThnScript(Game.GameData.ResolveDataPath(ct.Thorn));
                        gp.Thn = new Cutscene(new ThnScript[] { thn }, gp);
                    });
                    break;
                case UpdateRTCPacket rtc:
                    AddRTC(rtc.RTCs);
                    break;
                case MsnDialogPacket msndlg:
                    RunSync(() => {
                        RunDialog(msndlg.Lines);
                    });
                    break;
                case PlaySoundPacket psnd:
                    PlaySound(psnd.Sound);
                    break;
                case PlayMusicPacket mus:
                    PlayMusic(mus.Music);
                    break;
                case SpawnPlayerPacket p:
                    PlayerBase = null;
                    PlayerSystem = p.System;
                    PlayerPosition = p.Position;
                    PlayerOrientation = Matrix4x4.CreateFromQuaternion(p.Orientation);
                    SetSelfLoadout(p.Ship);
                    SceneChangeRequired();
                    break;
                case BaseEnterPacket b:
                    PlayerBase = b.Base;
                    SetSelfLoadout(b.Ship);
                    SceneChangeRequired();
                    AddRTC(b.RTCs);
                    break;
                case SpawnSolarPacket solar:
                    RunSync(() =>
                    {
                        foreach (var si in solar.Solars)
                        {
                            if (!objects.ContainsKey(si.ID))
                            {
                                var arch = Game.GameData.GetSolarArchetype(si.Archetype);
                                var go = new GameObject(arch, Game.ResourceManager, true);
                                go.StaticPosition = si.Position;
                                go.Transform = Matrix4x4.CreateFromQuaternion(si.Orientation) *
                                               Matrix4x4.CreateTranslation(si.Position);
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
                    break;
                case DestroyPartPacket p:
                    RunSync(() =>
                    {
                        objects[p.ID].DisableCmpPart(p.PartName);
                    });
                    break;
                case SpawnDebrisPacket p:
                    RunSync(() =>
                    {
                        var arch = Game.GameData.GetSolarArchetype(p.Archetype);
                        var mdl =
                            ((IRigidModelFile) arch.ModelFile.LoadFile(Game.ResourceManager)).CreateRigidModel(true);
                        var newpart = mdl.Parts[p.Part].Clone();
                        var newmodel = new RigidModel()
                        {
                            Root = newpart,
                            AllParts = new[] { newpart },
                            MaterialAnims = mdl.MaterialAnims,
                            Path = mdl.Path,
                        };
                        var go = new GameObject($"debris{p.ID}", newmodel, Game.ResourceManager, p.Part, p.Mass, true);
                        go.Transform = Matrix4x4.CreateFromQuaternion(p.Orientation) *
                                       Matrix4x4.CreateTranslation(p.Position);
                        go.World = gp.world;
                        go.Register(go.World.Physics);
                        gp.world.Objects.Add(go);
                        objects.Add(p.ID, go);
                    });
                    break;
                case SpawnObjectPacket p:
                    RunSync(() =>
                    {
                        var shp = Game.GameData.GetShip((int) p.Loadout.ShipCRC);
                        //Set up player object + camera
                        var newobj = new GameObject(shp, Game.ResourceManager);
                        newobj.Name = "NetPlayer " + p.ID;
                        newobj.Transform = Matrix4x4.CreateFromQuaternion(p.Orientation) *
                                           Matrix4x4.CreateTranslation(p.Position);
                        if(connection is GameNetClient) 
                            newobj.Components.Add(new CNetPositionComponent(newobj));
                        objects.Add(p.ID, newobj);
                        gp.world.Objects.Add(newobj);
                    });
                    break;
                case ObjectUpdatePacket p:
                    RunSync(() =>
                    {
                        foreach (var update in p.Updates)
                            UpdateObject(p.Tick, update);
                    });
                    break;
                case DespawnObjectPacket p:
                    RunSync(() =>
                    {
                        var despawn = objects[p.ID];
                        gp.world.Objects.Remove(despawn);
                        objects.Remove(p.ID);
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
            }
            else
            {
                var tr = obj.GetTransform();
                var pos = update.HasPosition ? update.Position : Vector3.Transform(Vector3.Zero, tr);
                var rot = update.HasOrientation ? update.Orientation : tr.ExtractRotation();
                obj.Transform = Matrix4x4.CreateFromQuaternion(rot) * Matrix4x4.CreateTranslation(pos);
            }
        }

        public void Launch() => connection.SendPacket(new LaunchPacket(), PacketDeliveryMethod.ReliableOrdered);
        
        public void ProcessConsoleCommand(string str) => connection.SendPacket(new ConsoleCommandPacket() {Command = str}, PacketDeliveryMethod.ReliableOrdered);
        

        public void Disconnected()
        {
            Game.ChangeState(new LuaMenu(Game));
        }

        public void OnExit()
        {
            connection.Shutdown();
        }
    }
}
