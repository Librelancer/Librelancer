// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

using LibreLancer.Ini;
using LibreLancer.Compatibility.GameData.Solar;

namespace LibreLancer.Compatibility.GameData
{
	public abstract class Archetype 
	{
		protected Section section;
		protected FreelancerData FLData;
        [Entry("nickname")]
        public string Nickname = "";
        [Entry("ids_name")]
        public int IdsName;
        [Entry("ids_info", Multiline = true)]
        public List<int> IdsInfo;
        [Entry("material_library", Multiline = true)]
		public List<string> MaterialPaths = new List<string>();
        [Entry("mass")]
		public float? Mass { get; private set; }
        [Entry("shape_name")]
		public string ShapeName { get; private set; }
        [Entry("solar_radius")]
		public float? SolarRadius { get; private set; }
        [Entry("da_archetype")]
		public string DaArchetypeName;
        [Entry("hit_pts")]
		public float? Hitpoints { get; private set; }
        [Entry("type")]
        public string Type;
        //TODO: I don't know what this is or what it does
        [Entry("phantom_physics")]
        public bool? PhantomPhysics;
        [Entry("loadout")]
		public string LoadoutName;
        //Set from parent ini
        public List<CollisionGroup> CollisionGroups = new List<CollisionGroup>();
        //Handled manually
        public List<DockSphere> DockingSpheres = new List<DockSphere>();
        [Entry("open_anim")]
        public string OpenAnim;
        [Entry("lodranges")]
        public float[] LODRanges;

        protected bool HandleEntry(Entry e)
        {
            if(e.Name.Equals("docking_sphere", StringComparison.InvariantCultureIgnoreCase)) {
                string scr = e.Count == 4 ? e[3].ToString() : null;
                DockingSpheres.Add(new DockSphere() { Name = e[0].ToString(), Hardpoint = e[1].ToString(), Radius = e[2].ToInt32(), Script = scr });
                return true;
            }
            return false;
        }

		public static Archetype FromSection(Section section, FreelancerData data)
		{
			if (section == null) throw new ArgumentNullException("section");

			Entry type = section ["type"];
			if (type == null) { //Find case-insensitive
				foreach (var entry in section)
					if (entry.Name.ToLowerInvariant () == "type") {
						type = entry;
						break;
					}
			}
            if (type == null) { 
                FLLog.Error("Ini","Missing type Entry in " + section.Name);
                return null;
            }
            if (type.Count < 1) {
                FLLog.Error("Ini","Invalid number of values in " + section.Name + " Entry type: " + type.Count);
                return null;
            }

			switch (type[0].ToString().ToLowerInvariant())
			{
			case "sun": return IniFile.FromSection<Sun>(section);
			case "planet": return IniFile.FromSection<Planet>(section);
			case "docking_ring": return IniFile.FromSection<DockingRing>(section);
			case "station": return IniFile.FromSection<Station>(section);
			case "jump_gate": return IniFile.FromSection<JumpGate>(section);
			case "satellite": return IniFile.FromSection<Satellite>(section);
			case "jump_hole": return IniFile.FromSection<JumpHole>(section);
			case "mission_satellite": return IniFile.FromSection<MissionSatellite>(section);
			case "non_targetable": return IniFile.FromSection<NonTargetable>(section);
			case "weapons_platform": return IniFile.FromSection<WeaponsPlatform>(section);
			case "tradelane_ring": return IniFile.FromSection<TradelaneRing>(section);
			case "waypoint": return IniFile.FromSection<Waypoint>(section);
			case "airlock_gate": return IniFile.FromSection<AirlockGate>(section);
			case "destroyable_depot": return IniFile.FromSection<DestroyableDepot>(section);
			default: FLLog.Error("Ini", "Invalid value in " + section.Name + " Entry type: " + section["type"][0]);
                    return null;
			}
		}
	}
}
