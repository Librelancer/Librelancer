using System;
using System.Collections.Generic;
using LibreLancer.Ini;
namespace LibreLancer.Compatibility.GameData.Ships
{
	public class Ship
	{
		public string Nickname;
		public string DaArchetypeName;
		public List<string> MaterialLibraries = new List<string>();
		public int Hitpoints;
		public int NanobotLimit;
		public int ShieldBatteryLimit;
		public int HoldSize;
		public int Mass;
		public int ShipClass;
		public string Type;

		public Ship (Section s, FreelancerData fldata)
		{
			foreach (Entry e in s) {
				switch (e.Name.ToLowerInvariant ()) {
				case "nickname":
					Nickname = e [0].ToString ();
					break;
				case "da_archetype":
					DaArchetypeName = VFS.GetPath (fldata.Freelancer.DataPath + e [0].ToString ());
					break;
				case "hit_pts":
					Hitpoints = e [0].ToInt32 ();
					break;
				case "nanobot_limit":
					NanobotLimit = e [0].ToInt32 ();
					break;
				case "shield_battery_limit":
					ShieldBatteryLimit = e [0].ToInt32 ();
					break;
				case "hold_size":
					HoldSize = e [0].ToInt32 ();
					break;
				case "mass":
					Mass = e [0].ToInt32 ();
					break;
				case "ship_class":
					ShipClass = e [0].ToInt32 ();
					break;
				case "type":
					Type = e [0].ToString ();
					break;
				case "material_library":
					MaterialLibraries.Add (VFS.GetPath (fldata.Freelancer.DataPath + e [0].ToString ()));
					break;
				}
			}
		}
	}
}

