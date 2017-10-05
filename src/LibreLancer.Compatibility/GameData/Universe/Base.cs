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
 * The Original Code is Starchart code (http://flapi.sourceforge.net/).
 * 
 * The Initial Developer of the Original Code is Malte Rupprecht (mailto:rupprema@googlemail.com).
 * Portions created by the Initial Developer are Copyright (C) 2011, 2012
 * the Initial Developer. All Rights Reserved.
 */

using System;
using System.Collections.Generic;

using LibreLancer.Ini;

namespace LibreLancer.Compatibility.GameData.Universe
{
	public class Base : UniverseElement
	{
		public string System { get; private set; }
		public string BGCSBaseRunBy { get; private set; }
		public string TerrainTiny { get; private set; }
		public string TerrainSml { get; private set; }
		public string TerrainMdm { get; private set; }
		public string TerrainLrg { get; private set; }
		public string TerrainDyna1 { get; private set; }
		public string TerrainDyna2 { get; private set; }
		public bool? AutosaveForbidden { get; private set; }

		public string StartRoom { get; private set; }

		public List<Room> Rooms { get; private set; }
		public Base (Section section, FreelancerData data) : base (data)
		{
			if (section == null) throw new ArgumentNullException("section");
			string file = null;
			foreach (Entry e in section)
			{
				switch (e.Name.ToLowerInvariant())
				{
				case "nickname":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (Nickname != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					Nickname = e[0].ToString();
					break;
				case "system":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (System != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					System = e[0].ToString();
					break;
				case "strid_name":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (StridName != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					StridName = data.Infocards.GetStringResource(e[0].ToInt32());
					break;
				case "file":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (file != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					file = e[0].ToString();
					break;
				case "bgcs_base_run_by":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (BGCSBaseRunBy != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					BGCSBaseRunBy = e[0].ToString();
					break;
				case "terrain_tiny":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (TerrainTiny != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					TerrainTiny = e[0].ToString();
					break;
				case "terrain_sml":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (TerrainSml != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					TerrainSml = e[0].ToString();
					break;
				case "terrain_mdm":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (TerrainMdm != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					TerrainMdm = e[0].ToString();
					break;
				case "terrain_lrg":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (TerrainLrg != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					TerrainLrg = e[0].ToString();
					break;
				case "terrain_dyna_01":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (TerrainDyna1 != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					TerrainDyna1 = e[0].ToString();
					break;
				case "terrain_dyna_02":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (TerrainDyna2 != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					TerrainDyna2 = e[0].ToString();
					break;
				case "autosave_forbidden":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (AutosaveForbidden != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					AutosaveForbidden = e[0].ToBoolean();
					break;
				default:
					throw new Exception("Invalid Entry in " + section.Name + ": " + e.Name);
				}
			}

			Rooms = new List<Room>();

			foreach (Section s in ParseFile(data.Freelancer.DataPath + file))
			{
				switch (s.Name.ToLowerInvariant())
				{
				case "baseinfo":
					foreach (Entry e in s)
					{
						switch (e.Name.ToLowerInvariant())
						{
						case "nickname":
							if (e.Count != 1) throw new Exception("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
							if (Name != null) FLLog.Warning("Base","Duplicate " + e.Name + " Entry in " + s.Name);
							Name = e[0].ToString();
							break;
						case "start_room":
							if (e.Count != 1) throw new Exception("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
							if (StartRoom != null) FLLog.Warning("Base", "Duplicate " + e.Name + " Entry in " + s.Name);
							StartRoom = e[0].ToString();
							break;
					    case "price_variance":
							FLLog.Error("Base", "Unimplemented: price_variance");
							break;
						default:
							throw new Exception("Invalid Entry in " + s.Name + ": " + e.Name);
						}
					}
					break;
				case "room":
					Rooms.Add(new Room(s, data));
					break;
				default:
					throw new Exception("Invalid Section in " + file + ": " + s.Name);
				}
			}
		}
	}
}