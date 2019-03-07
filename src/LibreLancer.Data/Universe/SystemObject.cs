// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Primitives;

using LibreLancer.Ini;

using LibreLancer.Data.Solar;
using LibreLancer.Data.Characters;

namespace LibreLancer.Data.Universe
{
	public class SystemObject : SystemPart
	{
		private UniverseIni universe;
		private StarSystem system;

		private bool atmosphere = false;
		public Color4? AmbientColor { get; private set; }

		private string archetypeName;
		private Archetype archetype;
		public Archetype Archetype
		{
			get
			{
				if (archetype == null) archetype = GameData.Solar.FindSolar(archetypeName);
				return archetype;
			}
		}

		public string Star { get; private set; }
		public int? AtmosphereRange { get; private set; }
		public Color4? BurnColor { get; private set; }

		private string baseName;
		private Base pBase;
		public Base Base
		{
			get
			{
				if (pBase == null) pBase = universe.FindBase(baseName);
				return pBase;
			}
		}

		public string MsgIdPrefix { get; private set; }
		public string JumpEffect { get; private set; }
		public string Behavior { get; private set; }
		public int? DifficultyLevel { get; private set; }
		public JumpReference Goto { get; private set; }

		public string LoadoutName;

		public string Pilot { get; private set; }

		private string dockWithName;

		public string DockWith
		{
			get
			{
				return dockWithName;
			}
		}


		public string Voice { get; private set; }
		public Costume SpaceCostume { get; private set; }
		public string Faction { get; private set; }

		public string PrevRing { get; private set; }

		private string nextRingName;
		public string NextRing
		{
			get
			{
				return nextRingName;
			}
		}

		public List<int> TradelaneSpaceName { get; private set; }

		private string parentName;
		private SystemObject parent;
		public SystemObject Parent
		{
			get
			{
				if (parent == null && parentName != null) parent = system.FindObject(parentName);
				return parent;
			}
		}

		public int InfoCardIds { get; private set; }

		public SystemObject(UniverseIni universe, StarSystem system, Section section, FreelancerData freelancerIni)
			: base(section, freelancerIni)
		{
			if (universe == null) throw new ArgumentNullException("universe");
			if (system == null) throw new ArgumentNullException("system");
			if (section == null) throw new ArgumentNullException("section");

			this.universe = universe;
			this.system = system;
			TradelaneSpaceName = new List<int>();

			foreach (Entry e in section)
			{
				if (!parentEntry(e))
				{
					switch (e.Name.ToLowerInvariant())
					{
					case "ambient_color":
					case "ambient":
						if (e.Count != 3) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (AmbientColor != null) FLLog.Warning("Ini","Duplicate " + e.Name + " Entry in " + section.Name);
						AmbientColor = new Color4(e[0].ToInt32() / 255f, e[1].ToInt32() / 255f, e[2].ToInt32() / 255f, 1f);
						break;
					case "archetype":
						if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (archetypeName != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						archetypeName = e[0].ToString();
						break;
					case "star":
						if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (Star != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						Star = e[0].ToString();
						break;
					case "atmosphere_range":
						if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (AtmosphereRange != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						AtmosphereRange = e[0].ToInt32();
						break;
					case "burn_color":
						if (e.Count != 3) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (BurnColor != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						BurnColor = new Color4(e[0].ToInt32() / 255f, e[1].ToInt32() / 255f, e[2].ToInt32() / 255f, 1f);
						break;
					case "base":
						if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (baseName != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						baseName = e[0].ToString();
						break;
					case "msg_id_prefix":
						if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (MsgIdPrefix != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						MsgIdPrefix = e[0].ToString();
						break;
					case "jump_effect":
						if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (JumpEffect != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						JumpEffect = e[0].ToString();
						break;
					case "behavior":
						if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (Behavior != null && Behavior != e[0].ToString()) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						Behavior = e[0].ToString();
						break;
					case "difficulty_level":
						if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (DifficultyLevel != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						DifficultyLevel = e[0].ToInt32();
						break;
					case "goto":
						if (e.Count != 3) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (Goto != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						Goto = new JumpReference(e[0].ToString(), e[1].ToString(), e[2].ToString());
						break;
					case "loadout":
						if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (LoadoutName != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						LoadoutName = e[0].ToString();
						break;
					case "pilot":
						if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (Pilot != null && Pilot != e[0].ToString()) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						Pilot = e[0].ToString();
						break;
					case "dock_with":
						if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (dockWithName != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						dockWithName = e[0].ToString();
						break;
					case "voice":
						if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (Voice != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						Voice = e[0].ToString();
						break;
					case "space_costume":
						if (e.Count < 1 /*|| e.Count > 3*/) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (SpaceCostume != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						SpaceCostume = new Costume(e[0].ToString(), e[1].ToString(), e.Count >= 3 ? e[2].ToString() : string.Empty, freelancerIni);
						break;
					case "faction":
						if (e.Count != 1) FLLog.Warning("Ini", "Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (Faction != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						Faction = e[0].ToString();
						break;
					case "prev_ring":
						if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (PrevRing != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						PrevRing = e[0].ToString();
						break;
					case "next_ring":
						if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (nextRingName != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						nextRingName = e[0].ToString();
						break;
					case "tradelane_space_name":
						if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
                        TradelaneSpaceName.Add(e[0].ToInt32());
						break;
					case "parent":
						if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (parentName != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						parentName = e[0].ToString();
						break;
					case "info_card_ids":
					case "info_card":
					case "info_ids":
						if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (InfoCardIds != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
                        InfoCardIds = e[0].ToInt32();
						break;
					case "ring":
						if (e.Count != 2) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						//if ( != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						//TODO
						break;
					case "260800": // Strange error
						break;
					case "rot":
						FLLog.Warning("SystemObject", "unimplemented: rot");
						break;
					default:
						throw new Exception("Invalid Entry in " + section.Name + ": " + e.Name);
					}
				}
			}
		}
	}
}
