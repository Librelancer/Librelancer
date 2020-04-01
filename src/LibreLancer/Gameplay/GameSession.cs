// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Collections.Generic;
using LibreLancer.Data.Missions;
using LibreLancer.Utf.Cmp;
using Lidgren.Network;

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


    public class GameSession
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
		public GameSession(FreelancerGame g, IPacketConnection connection)
		{
			Game = g;
            this.connection = connection;
        }

        public void AddRTC(string path)
        {
            var rtc = new Data.Missions.StoryCutsceneIni(Game.GameData.Ini.Freelancer.DataPath + path, Game.GameData.VFS);
            ActiveCutscenes.Add(rtc);
        }

        public void RoomEntered(string room, string bse)
        {
            
        }

        private bool hasChanged = false;
        void SceneChangeRequired()
        {
            if (PlayerBase != null)
            {
                Game.ChangeState(new RoomGameplay(Game, this, PlayerBase));
                hasChanged = true;
            }
            else
            {
                worldReady = false;
                toAdd = new List<GameObject>();
                gp = new SpaceGameplay(Game, this);
                Game.ChangeState(gp);
                hasChanged = true;
            }
        }

        SpaceGameplay gp;
        bool worldReady = false;
        List<GameObject> toAdd = new List<GameObject>();
        Dictionary<int, GameObject> objects = new Dictionary<int, GameObject>();

        public bool Update()
        {
            hasChanged = false;
            UpdatePackets();
            return hasChanged;
        }

        Queue<Action<SpaceGameplay>> gameplayActions = new Queue<Action<SpaceGameplay>>();
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
                act(gp);
        }

        public void WorldReady()
        {
            worldReady = true;
            foreach (var obj in toAdd)
            {
                gp.world.Objects.Add(obj);
            }
            toAdd = new List<GameObject>();
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
        public void WaitStart()
        {
            IPacket packet;
            bool started = false;
            while (!started)
            {
                while (connection.PollPacket(out packet))
                {
                    HandlePacket(packet);
                    if (packet is BaseEnterPacket || packet is SpawnPlayerPacket)
                        started = true;
                }
                Game.Yield();
            }
        }

        void AddGameplayAction(Action<SpaceGameplay> gp)
        {
            gameplayActions.Enqueue(gp);
        }

        void PlaySound(string sound) => audioActions.Enqueue(() => Game.Sound.PlaySound(sound));
        void PlayMusic(string music) => audioActions.Enqueue(() => Game.Sound.PlayMusic(music));

        void RunDialog(NetDlgLine[] lines, int index = 0)
        {
            if (index >= lines.Length) return;
            Game.Sound.PlayVoiceLine(lines[index].Voice, lines[index].Hash, () =>
            {
                connection.SendPacket(new LineSpokenPacket() { Hash = lines[index].Hash }, NetDeliveryMethod.ReliableOrdered);
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
                    AddGameplayAction(gp => {
                        var thn = new ThnScript(Game.GameData.ResolveDataPath(ct.Thorn));
                        gp.Thn = new Cutscene(new ThnScript[] { thn }, gp);
                    });
                    break;
                case AddRTCPacket rtc:
                    AddRTC(rtc.RTC);
                    break;
                case MsnDialogPacket msndlg:
                    AddGameplayAction(gp =>
                    {
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
                    break;
                case SpawnObjectPacket p:
                    var shp = Game.GameData.GetShip((int)p.Loadout.ShipCRC);
                    //Set up player object + camera
                    var newobj = new GameObject(shp, Game.ResourceManager);
                    newobj.Name = "NetPlayer " + p.ID;
                    newobj.Transform = Matrix4x4.CreateFromQuaternion(p.Orientation) *
                        Matrix4x4.CreateTranslation(p.Position);
                    objects.Add(p.ID, newobj);
                    if (worldReady)
                    {
                        gp.world.Objects.Add(newobj);
                    }
                    else
                        toAdd.Add(newobj);
                    break;
                case ObjectUpdatePacket p:
                    foreach (var update in p.Updates)
                        UpdateObject(update);
                    break;
                case DespawnObjectPacket p:
                    var despawn = objects[p.ID];
                    if(worldReady)
                        gp.world.Objects.Remove(despawn);
                    else
                        toAdd.Remove(despawn);
                    objects.Remove(p.ID);
                    break;
                default:
                    if (ExtraPackets != null) ExtraPackets(pkt);
                    else FLLog.Error("Network", "Unknown packet type " + pkt.GetType().ToString());
                    break;
            }
        }

        void UpdateObject(PackedShipUpdate update)
        {
            var obj = objects[update.ID];
            var tr = obj.GetTransform();
            var pos = update.HasPosition ? update.Position : Vector3.Transform(Vector3.Zero, tr);
            var rot = update.HasOrientation ? update.Orientation : tr.ExtractRotation();
            obj.Transform = Matrix4x4.CreateFromQuaternion(rot) * Matrix4x4.CreateTranslation(pos);
        }

        public void Launch()
        {
            connection.SendPacket(new LaunchPacket(), NetDeliveryMethod.ReliableOrdered);
        }
        
        public void ProcessConsoleCommand(string str)
		{
			//TODO: Chat 
		}

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
