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
