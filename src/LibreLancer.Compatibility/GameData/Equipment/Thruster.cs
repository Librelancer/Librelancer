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
using LibreLancer.Ini;
namespace LibreLancer.Compatibility.GameData.Equipment
{
	public class Thruster : AbstractEquipment
	{
		public string Particles;
		public string DaArchetype;
		public string MaterialLibrary;
		public string HpParticles;
		public int MaxForce;
		public int PowerUsage;
		public int IdsName;
		public int IdsInfo;
		public int Hitpoints;

		public Thruster(Section section)
			: base(section)
		{
			foreach (Entry e in section)
			{
				if (!parentEntry(e))
				{
					switch (e.Name.ToLowerInvariant())
					{
						case "ids_name":
							IdsName = e[0].ToInt32();
							break;
						case "ids_info":
							IdsInfo = e[0].ToInt32();
							break;
						case "particles":
							Particles = e[0].ToString();
							break;
						case "hp_particles":
							HpParticles = e[0].ToString();
							break;
						case "max_force":
							MaxForce = e[0].ToInt32();
							break;
						case "power_usage":
							PowerUsage = e[0].ToInt32();
							break;
						case "da_archetype":
							DaArchetype = e[0].ToString();
							break;
						case "material_library":
							MaterialLibrary = e[0].ToString();
							break;
						case "hit_pts":
							Hitpoints = e[0].ToInt32();
							break;
					}
				}
			}
		}
	}
}
