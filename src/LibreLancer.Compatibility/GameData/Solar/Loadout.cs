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
using System.Linq;

using LibreLancer.Ini;
using LibreLancer.Compatibility.GameData.Equipment;

namespace LibreLancer.Compatibility.GameData.Solar
{
	public class Loadout
	{
		public string Nickname { get; private set; }
		public Archetype Archetype { get; private set; }

		//public List<Equip> Equip { get; private set; }
		public Dictionary<string, AbstractEquipment> Equip { get; private set; }

		public Loadout(Section section, FreelancerData freelancerIni)
		{
			if (section == null) throw new ArgumentNullException("section");

			//Equip = new List<Equip>();
			Equip = new Dictionary<string, AbstractEquipment>();

			int emptyHp = 1;
			foreach (Entry e in section)
			{
				switch (e.Name.ToLowerInvariant())
				{
				case "nickname":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (Nickname != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					Nickname = e[0].ToString();
					break;
				case "archetype":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (Archetype != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					Archetype = freelancerIni.Solar.FindSolar(e[0].ToString());
					break;
				case "equip":
					//if (e.Count < 1 || e.Count > 2) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count); //TODO: Reverse-engineer this properly
					//HACK: Come up with a proper way of handling this
					string key = e.Count == 2 ? e[1].ToString() : "__noHardpoint" + (emptyHp++).ToString("d2");
						if (e.Count == 2 && e[1].ToString().Trim() == "") key = "__noHardpoint" + (emptyHp++).ToString("d2");
					if (!Equip.ContainsKey(key)) Equip.Add(key, freelancerIni.Equipment.FindEquipment(e[0].ToString()));
					break;
				case "cargo":
					// TODO: Loadout cargo
					break;
				case "hull":
					// TODO: Loadout hull?
					break;
				case "addon":
					// TODO: Loadout addon?
					break;
                case "hull_damage":
                        //TODO: Is this real or a disco bug?
                    break;
				default:
					FLLog.Error("Loadout","Invalid Entry in " + section.Name + ": " + e.Name);
                    break;
				}
			}
		}
	}
}
