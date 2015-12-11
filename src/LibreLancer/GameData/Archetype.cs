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
using System.IO;
using System.Xml;

using OpenTK;

using LibreLancer.Ini;

using LibreLancer.Utf.Mat;
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf.Vms;
using LibreLancer.GameData.Solar;
//using LibreLancer.GameData.Universe;

namespace LibreLancer.GameData
{
	public abstract class Archetype : IDrawable, ILibFile
	{
		protected Section section;
		protected FreelancerData GameData;

		public string Nickname { get; private set; }
		public string IdsName { get; private set; }
		public List<XmlDocument> IdsInfo { get; private set; }

		private List<string> materialPaths = new List<string>();
		private List<MatFile> materials = null;
		public List<MatFile> Materials
		{
			get
			{
				if (materials == null)
				{
					materials = new List<MatFile>();
					foreach (string path in materialPaths) materials.Add(new MatFile(GameData.Freelancer.DataPath + path, this));
				}
				return materials;
			}
		}

		private List<string> texturePaths = new List<string>();
		private List<TxmFile> textures = null;
		public List<TxmFile> Textures
		{
			get
			{
				if (textures == null)
				{
					textures = new List<TxmFile>();
					foreach (string path in texturePaths) textures.Add(new TxmFile(GameData.Freelancer.DataPath + path));
				}
				return textures;
			}
		}

		public float? Mass { get; private set; }
		public string ShapeName { get; private set; }

		public float? SolarRadius { get; private set; }

		private string daArchetypeName;
		private IDrawable daArchetype;
		public IDrawable DaArchetype
		{
			get
			{
				if (daArchetype == null)
				{
					if (daArchetypeName.EndsWith(".sph", StringComparison.OrdinalIgnoreCase))
					{
						daArchetype = new SphFile(GameData.Freelancer.DataPath + daArchetypeName, this);
					}
					else if (daArchetypeName.EndsWith(".3db", StringComparison.OrdinalIgnoreCase))
					{
						daArchetype = new ModelFile(GameData.Freelancer.DataPath + daArchetypeName, this);
					}
					else if (daArchetypeName.EndsWith(".cmp", StringComparison.OrdinalIgnoreCase))
					{
						daArchetype = new CmpFile(GameData.Freelancer.DataPath + daArchetypeName, this);
					}
				}
				return daArchetype;
			}
		}

		private string loadoutName;
		private Loadout loadout;
		public Loadout Loadout
		{
			get
			{
				if (loadout == null) loadout = GameData.Loadouts.FindLoadout(loadoutName);
				return loadout;
			}
		}

		public List<CollisionGroup> CollisionGroups { get; private set; }

		protected Archetype(Section section, FreelancerData data)
		{
			if (section == null) throw new ArgumentNullException("section");
			GameData = data;
			this.section = section;

			IdsInfo = new List<XmlDocument>();
			CollisionGroups = new List<CollisionGroup>();
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
				IdsName = GameData.Infocards.GetStringResource(e[0].ToInt32());
				break;
			case "ids_info":
				if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
				IdsInfo.Add(GameData.Infocards.GetXmlResource(e[0].ToInt32()));
				break;
			case "type":
				break;
			case "material_library":
				if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
				string path = e[0].ToString();
				switch (Path.GetExtension(path))
				{
				case ".mat":
					materialPaths.Add(path);
					break;
				case ".txm":
					texturePaths.Add(path);
					break;
				default:
					throw new Exception("Invalid value in " + section.Name + " Entry " + e.Name + ": " + path);
				}
				break;
			case "mass":
				if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
				if (Mass != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
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
				if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
				if (daArchetype != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
				daArchetypeName = e[0].ToString();
				break;
			case "loadout":
				if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
				if (loadoutName != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
				loadoutName = e[0].ToString();
				break;
			default: return false;
			}

			return true;
		}

		public static Archetype FromSection(Section section, FreelancerData data)
		{
			if (section == null) throw new ArgumentNullException("section");

			Entry type = section["type"];
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

		public void Initialize(ResourceCache cache)
		{
			DaArchetype.Initialize(cache);
		}

		public void Resized()
		{
			DaArchetype.Resized();
		}

		public void Update(Camera camera)
		{
			DaArchetype.Update (camera);
		}

		public void Draw (Matrix4 World, Lighting lights)
		{
			DaArchetype.Draw (World, lights);
		}

		public TextureData FindTexture(string name)
		{
			foreach (TxmFile txmFile in Textures)
			{
				TextureData texture = txmFile.FindTexture(name);
				if (texture != null) return texture;
			}

			return null;
		}

		public Material FindMaterial(uint materialId)
		{
			foreach (MatFile matFile in Materials)
			{
				Material material = matFile.FindMaterial(materialId);
				if (material != null) return material;
			}

			return null;
		}

		public VMeshData FindMesh(uint vMeshLibId)
		{
			return null;
		}
	}
}
