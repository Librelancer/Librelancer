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
using LibreLancer.Ini;
namespace LibreLancer.Compatibility.GameData.Ships
{
	public class Ship
	{
		public int IdsName;
		public int IdsInfo;
		public int? IdsInfo1;
		public int? IdsInfo2;
		public int? IdsInfo3;
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

		public Vector3 CameraOffset;
		public float CameraAngularAcceleration;
		public float CameraHorizontalTurnAngle;
		public float CameraVerticalTurnUpAngle;
		public float CameraVerticalTurnDownAngle;
		public float CameraTurnLookAheadSlerpAmount;

		public Ship (Section s, FreelancerData fldata)
		{
			foreach (Entry e in s) {
				switch (e.Name.ToLowerInvariant ()) {
				case "nickname":
					Nickname = e [0].ToString ();
					break;
				case "ids_name":
					IdsName = e[0].ToInt32();
					break;
				case "ids_info":
					IdsInfo = e[0].ToInt32();
					break;
				case "ids_info1":
					IdsInfo1 = e[0].ToInt32();
					break;
				case "ids_info2":
					IdsInfo2 = e[0].ToInt32();
					break;
				case "ids_info3":
					IdsInfo3 = e[0].ToInt32();
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
				case "camera_offset":
					CameraOffset = new Vector3(0, e[0].ToSingle(), e[1].ToSingle());
					break;
				case "camera_angular_acceleration":
					CameraAngularAcceleration = e[0].ToSingle();
					break;
				case "camera_horizontal_turn_angle":
					CameraHorizontalTurnAngle = e[0].ToSingle();
					break;
				case "camera_vertical_turn_up_angle":
					CameraVerticalTurnUpAngle = e[0].ToSingle();
					break;
				case "camera_vertical_turn_down_angle":
					CameraVerticalTurnDownAngle = e[0].ToSingle();
					break;
				case "camera_turn_look_ahead_slerp_amount":
					CameraTurnLookAheadSlerpAmount = e[0].ToSingle();
					break;
				}
			}
		}
	}
}

