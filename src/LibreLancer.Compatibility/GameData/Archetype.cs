// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and confiditons defined in
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

		public string Nickname { get; private set; }
		public string IdsName { get; private set; }
		public List<string> IdsInfo { get; private set; }

		public List<string> MaterialPaths = new List<string>();

		public List<string> TexturePaths = new List<string>();

		public float? Mass { get; private set; }
		public string ShapeName { get; private set; }

		public float? SolarRadius { get; private set; }

		public string DaArchetypeName;

		public float? Hitpoints { get; private set; }

		//TODO: I don't know what this is or what it does
		public bool? PhantomPhysics { get; private set; }

		public string LoadoutName;

		public List<CollisionGroup> CollisionGroups { get; private set; }

		public List<DockSphere> DockingSpheres { get; private set; }

		public string OpenAnim { get; private set; }

		public float[] LODRanges { get; private set; }

		protected Archetype(Section section, FreelancerData data)
		{
			if (section == null) throw new ArgumentNullException("section");
			FLData = data;
			this.section = section;

			IdsInfo = new List<string>();
			CollisionGroups = new List<CollisionGroup>();
			DockingSpheres = new List<DockSphere>();
		}

		protected virtual bool parentEntry(Entry e)
		{
			switch (e.Name.ToLowerInvariant())
			{
			case "nickname":
				if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
				if (Nickname != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
				Nickname = e[0].ToString();
				break;
			case "ids_name":
				if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
				if (IdsName != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
				IdsName = FLData.Infocards.GetStringResource(e[0].ToInt32());
				break;
			case "ids_info":
				if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
				IdsInfo.Add(FLData.Infocards.GetXmlResource(e[0].ToInt32()));
				break;
			case "hit_pts":
				if (e.Count != 1) throw new Exception ("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
				Hitpoints = e [0].ToSingle ();
				break;
			case "phantom_physics":
				if (e.Count != 1) throw new Exception ("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
				PhantomPhysics = e [0].ToBoolean ();
				break;
			case "type":
				break;
			case "material_library":
				if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
				string path = e[0].ToString();
				switch (Path.GetExtension(path))
				{
				case ".mat":
					MaterialPaths.Add (VFS.GetPath (FLData.Freelancer.DataPath + path));
					break;
				case ".txm":
					TexturePaths.Add (VFS.GetPath (FLData.Freelancer.DataPath + path));
					break;
				default:
					throw new Exception("Invalid value in " + section.Name + " Entry " + e.Name + ": " + path);
				}
				break;
			case "mass":
				if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
				//if (Mass != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name); //Hack around discovery errors
				Mass = e[0].ToSingle();
				break;
			case "shape_name":
				if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
				if (ShapeName != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
				ShapeName = e[0].ToString();
				break;
			case "solar_radius":
				if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
				if (SolarRadius != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
				SolarRadius = e[0].ToSingle();
				break;
			case "da_archetype":
				if (e.Count != 1)
					throw new Exception ("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
				if (DaArchetypeName != null)
					throw new Exception ("Duplicate " + e.Name + " Entry in " + section.Name);
				DaArchetypeName = VFS.GetPath (FLData.Freelancer.DataPath + e [0].ToString ());
				break;
			case "loadout":
				if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
				if (LoadoutName != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
				LoadoutName = e[0].ToString();
				break;
			case "docking_sphere":
					string scr = e.Count == 4 ? e[3].ToString() : null;
				DockingSpheres.Add(new DockSphere() { Name = e[0].ToString(), Hardpoint = e[1].ToString(), Radius = e[2].ToInt32(), Script = scr });
				break;
			case "open_anim":
				OpenAnim = e[0].ToString();
				break;
			case "lodranges":
				LODRanges = new float[e.Count];
				for (int i = 0; i < e.Count; i++) LODRanges[i] = e[i].ToSingle();
				break;
			default: return false;
			}

			return true;
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
			if (type == null) throw new Exception("Missing type Entry in " + section.Name);
			if (type.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry type: " + type.Count);

			switch (type[0].ToString().ToLowerInvariant())
			{
			case "sun": return new Sun(section, data);
			case "planet": return new Planet(section, data);
			case "docking_ring": return new DockingRing(section, data);
			case "station": return new Station(section, data);
			case "jump_gate": return new JumpGate(section, data);
			case "satellite": return new Satellite(section, data);
			case "jump_hole": return new JumpHole(section, data);
			case "mission_satellite": return new MissionSatellite(section, data);
			case "non_targetable": return new NonTargetable(section, data);
			case "weapons_platform": return new WeaponsPlatform(section, data);
			case "tradelane_ring": return new TradelaneRing(section, data);
			case "waypoint": return new Waypoint(section, data);
			case "airlock_gate": return new AirlockGate(section, data);
			case "destroyable_depot": return new DestroyableDepot(section, data);
			default: throw new Exception("Invalid value in " + section.Name + " Entry type: " + section["type"][0]);
			}
		}
	}
}
