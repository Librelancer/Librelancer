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

namespace LibreLancer.Compatibility.GameData.Universe
{
	public class AsteroidField : ZoneReference
	{
		public Field Field { get; private set; }
		public List<CubeAsteroid> Cube { get; private set; }
		public Band Band { get; private set; }
		public Band ExclusionBand { get; private set; }
		public AsteroidBillboards AsteroidBillboards { get; private set; }
		public List<DynamicAsteroids> DynamicAsteroids { get; private set; }
		public List<LootableZone> LootableZones { get; private set; }

		public AsteroidField(StarSystem parent, Section section, FreelancerData data)
			: base(parent, section, data)
		{
			Cube = new List<CubeAsteroid>();
			DynamicAsteroids = new List<DynamicAsteroids>();
			LootableZones = new List<LootableZone>();

			foreach (Section s in ParseFile(data.Freelancer.DataPath + file))
			{
				switch (s.Name.ToLowerInvariant())
				{
				case "texturepanels":
					TexturePanels = new TexturePanelsRef(s, data);
					break;
				case "properties":
					foreach (Entry e in s)
					{
						switch (e.Name.ToLowerInvariant())
						{
						case "flag":
							if (e.Count != 1) throw new Exception("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
							Properties.Add(e[0].ToString());
							break;
						default:
							throw new Exception("Invalid Entry in " + s.Name + ": " + e.Name);
						}
					}
					break;
				case "exclusion zones":
					foreach (Entry e in s)
					{
						switch (e.Name.ToLowerInvariant())
						{
						case "exclusion":
						case "exclude":
							if (e.Count != 1) throw new Exception("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
							ExclusionZones.Add(new ExclusionZone(parent, e[0].ToString()));
							break;
						case "fog_far":
							if (e.Count != 1) throw new Exception("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
							if (ExclusionZones.Count == 0) throw new Exception(e.Name + " before exclusion");
							ExclusionZones[ExclusionZones.Count - 1].FogFar = e[0].ToSingle();
							break;
						case "zone_shell":
							if (e.Count != 1) throw new Exception("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
							if (ExclusionZones.Count == 0) throw new Exception(e.Name + " before exclusion");
							break;
						case "shell_scalar":
							if (e.Count != 1) throw new Exception("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
							if (ExclusionZones.Count == 0) throw new Exception(e.Name + " before exclusion");
							ExclusionZones[ExclusionZones.Count - 1].ShellScalar = e[0].ToSingle();
							break;
						case "max_alpha":
							if (e.Count != 1) throw new Exception("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
							if (ExclusionZones.Count == 0) throw new Exception(e.Name + " before exclusion");
							ExclusionZones[ExclusionZones.Count - 1].MaxAlpha = e[0].ToSingle();
							break;
						case "color":
							if (e.Count != 3) throw new Exception("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
							if (ExclusionZones.Count == 0) throw new Exception(e.Name + " before exclusion");
							ExclusionZones[ExclusionZones.Count - 1].Color = new Color4(e[0].ToInt32() / 255f, e[0].ToInt32() / 255f, e[0].ToInt32() / 255f, 1f);
							break;
						case "exclusion_tint":
							if (e.Count != 3) throw new Exception("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
							if (ExclusionZones.Count == 0) throw new Exception(e.Name + " before exclusion");
							ExclusionZones[ExclusionZones.Count - 1].Tint = new Color3f(e[0].ToInt32() / 255f, e[0].ToInt32() / 255f, e[0].ToInt32() / 255f);
							break;
						case "exclude_billboards":
							if (e.Count != 1) throw new Exception("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
							if (ExclusionZones.Count == 0) throw new Exception(e.Name + " before exclusion");
							ExclusionZones[ExclusionZones.Count - 1].ExcludeBillboards = e[0].ToInt32();
							break;
						case "exclude_dynamic_asteroids":
							if (e.Count != 1) throw new Exception("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
							if (ExclusionZones.Count == 0) throw new Exception(e.Name + " before exclusion");
							ExclusionZones[ExclusionZones.Count - 1].ExcludeDynamicAsteroids = e[0].ToInt32();
							break;
						case "empty_cube_frequency":
							if (e.Count != 1) throw new Exception("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
							if (ExclusionZones.Count == 0) throw new Exception(e.Name + " before exclusion");
							ExclusionZones[ExclusionZones.Count - 1].EmptyCubeFrequency = e[0].ToSingle();
							break;
						case "billboard_count":
							if (e.Count != 1) throw new Exception("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
							if (ExclusionZones.Count == 0) throw new Exception(e.Name + " before exclusion");
							ExclusionZones[ExclusionZones.Count - 1].BillboardCount = e[0].ToInt32();
							break;
						default:
							throw new Exception("Invalid Entry in " + s.Name + ": " + e.Name);
						}
					}
					break;
				case "field":
					Field = new Field(s);
					break;
				case "cube":
					foreach (Entry e in s)
					{
						switch (e.Name.ToLowerInvariant())
						{
						case "asteroid":
							if (e.Count < 7 || e.Count > 8) throw new Exception("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
							Cube.Add(new CubeAsteroid(e));
							break;
						}
					}
					break;
				case "band":
					Band = new Band(parent, s);
					break;
				case "exclusionband":
					ExclusionBand = new Band(parent, s);
					break;
				case "asteroidbillboards":
					AsteroidBillboards = new AsteroidBillboards(s);
					break;
				case "dynamicasteroids":
					DynamicAsteroids.Add(new DynamicAsteroids(s));
					break;
				case "lootablezone":
					LootableZones.Add(new LootableZone(s));
					break;
				default:
					throw new Exception("Invalid Section in " + file + ": " + s.Name);
				}
			}
		}
	}
}