// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
using System.Collections.Generic;

namespace LibreLancer
{
	public class GameSession
	{
        public long Credits;
		public string PlayerShip;
		public List<string> PlayerComponents = new List<string>();
		public Dictionary<string, string> MountedEquipment = new Dictionary<string, string>();
		public FreelancerGame Game;
		public string PlayerSystem;
		public string PlayerBase;
		public Vector3 PlayerPosition;
		public Matrix3 PlayerOrientation;
        public int MissionNum;

		public GameSession(FreelancerGame g)
		{
			Game = g;
            Credits = 2000;
			PlayerShip = "li_elite";
            PlayerBase = "li01_01_base";
			PlayerSystem = "li01";
			PlayerPosition = new Vector3(-31000, 0, -26755);
			PlayerOrientation = Matrix3.Identity;
			MountedEquipment.Add("hpthruster01", "ge_s_thruster_02");
            MountedEquipment.Add("hpweapon01", "li_gun01_mark01");
            MountedEquipment.Add("hpweapon02", "li_gun01_mark01");
            MountedEquipment.Add("hpweapon03", "li_gun01_mark01");
            MountedEquipment.Add("hpweapon04", "li_gun01_mark01");
            MountedEquipment.Add("HpContrail01", "contrail01");
            MountedEquipment.Add("HpContrail02", "contrail01");
        }
        
        public void LoadFromPath(string path)
        {
            var sg = Data.Save.SaveGame.FromFile(path);
            PlayerPosition = sg.Player.Position;
            PlayerSystem = sg.Player.System;
            PlayerBase = sg.Player.Base;
            Credits = sg.Player.Money;
            MissionNum = sg.StoryInfo?.MissionNum ?? 0;
            if (Game.GameData.Ini.ContentDll.AlwaysMission13) MissionNum = 14;
            if (sg.Player.ShipArchetype != null)
                PlayerShip = sg.Player.ShipArchetype;
            else
                PlayerShip = Game.GameData.GetShip(sg.Player.ShipArchetypeCrc).Nickname;
            
            MountedEquipment = new Dictionary<string, string>();
            foreach(var eq in sg.Player.Equip)
            {
                if (eq.Hardpoint != null && eq.EquipName != null)
                    MountedEquipment.Add(eq.Hardpoint, eq.EquipName);
            }
        }

        MissionRuntime msnrun;

        public void Start()
        {
            if(MissionNum != 0 && (MissionNum - 1) < Game.GameData.Ini.Missions.Count) {
                msnrun = new MissionRuntime(Game.GameData.Ini.Missions[MissionNum - 1], this);
            }

            if (PlayerBase != null)
                Game.ChangeState(new RoomGameplay(Game, this, PlayerBase));
            else
                Game.ChangeState(new SpaceGameplay(Game, this));
        }

        string forcedLand = null;
        public bool Update(SpaceGameplay gameplay, TimeSpan elapsed)
        {
            forcedLand = null;
            if (msnrun != null)
                msnrun.Update(gameplay, elapsed);
            if(forcedLand != null)
            {
                Game.ChangeState(new RoomGameplay(Game, this, forcedLand));
                return true;
            }
            return false;
        }




        public void JumpTo(string system, string exitpos)
		{
			//Find object
			var sys = Game.GameData.GetSystem(system);
			var ep = exitpos.ToLowerInvariant();
			var obj = sys.Objects.Where((o) => o.Nickname.ToLowerInvariant() == ep).First();
			//Setup player
			PlayerSystem = system;
			PlayerOrientation = obj.Rotation == null ? Matrix3.Identity : new Matrix3(obj.Rotation.Value);
			PlayerPosition = Vector3.Transform(new Vector3(0, 0, 500), PlayerOrientation) + obj.Position; //TODO: This is bad
			//Switch
			Game.ChangeState(new SpaceGameplay(Game, this));
		}

        public void LaunchFrom(string _base)
        {
            var b = Game.GameData.GetBase(_base);
            var sys = Game.GameData.GetSystem(b.System);
            var obj = sys.Objects.FirstOrDefault((o) =>
            {
                return (o.Dock != null &&
                    o.Dock.Kind == DockKinds.Base &&
                    o.Dock.Target.Equals(_base, StringComparison.OrdinalIgnoreCase));
            });
            PlayerSystem = b.System;
            PlayerOrientation = Matrix3.Identity;
            PlayerPosition = Vector3.Zero;
            if (obj == null) {
                FLLog.Error("Base", "Can't find object in " + sys + " docking to " + b);
            }
            else
            {
                PlayerPosition = obj.Position;
                PlayerOrientation = obj.Rotation == null ? Matrix3.Identity : new Matrix3(obj.Rotation.Value);
                PlayerPosition = Vector3.Transform(new Vector3(0, 0, 500), PlayerOrientation) + obj.Position; //TODO: This is bad
            }
            Game.ChangeState(new SpaceGameplay(Game, this));
        }

        public void ForceLand(string str)
        {
            forcedLand = str;
        }

        public void ProcessConsoleCommand(string str)
		{
			var split = str.Split(' ');
            switch (split[0])
            {
                case "base":
                    Game.ChangeState(new RoomGameplay(Game, this, split[1]));
                    break;
                case "play":
                    Game.Sound.PlaySound(split[1]);
                    break;
			}
		}

	}
}
