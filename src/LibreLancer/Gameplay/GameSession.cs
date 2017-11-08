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
using System.Collections.Generic;
namespace LibreLancer
{
	public class GameSession
	{
		public string PlayerShip;
		public List<string> PlayerComponents = new List<string>();
		public Dictionary<string, string> MountedEquipment = new Dictionary<string, string>();
		public FreelancerGame Game;
		public GameSession(FreelancerGame g)
		{
			Game = g;
			PlayerShip = "li_elite";
			MountedEquipment.Add("hpthruster01", "ge_s_thruster_02");
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
