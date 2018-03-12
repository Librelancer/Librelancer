/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2017
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.Linq;
using System.Collections.Generic;
namespace LibreLancer
{
	public class GameSession
	{
		public string PlayerShip;
		public List<string> PlayerComponents = new List<string>();
		public Dictionary<string, string> MountedEquipment = new Dictionary<string, string>();
		public FreelancerGame Game;
		public string PlayerSystem;
		public string PlayerBase;
		public Vector3 PlayerPosition;
		public Matrix3 PlayerOrientation;
		public GameSession(FreelancerGame g)
		{
			Game = g;
			PlayerShip = "li_elite";
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

		public void ProcessConsoleCommand(string str)
		{
			var split = str.Split(' ');
			switch (split[0])
			{
				case "base":
					Game.ChangeState(new RoomGameplay(Game, this, split[1]));
					break;
			}
		}

	}
}
