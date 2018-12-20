// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Ini;

namespace LibreLancer.Data.Universe
{
	public class Zone : SystemPart
	{
		public ZoneShape? Shape { get; private set; }
		public List<IValue> AttackIds { get; private set; }
		public int? TradelaneAttack { get; private set; }
		public int? PropertyFlags { get; private set; }
		public Color4? PropertyFogColor { get; private set; }
		public string Music { get; private set; }
		public float? EdgeFraction { get; private set; }
		public string Spacedust { get; private set; }
		public int? SpacedustMaxparticles { get; private set; }
		public float? Interference { get; private set; }
		public float? PowerModifier { get; private set; }
		public float? DragModifier { get; private set; }
		public List<string> Comment { get; private set; }
		public int? LaneId { get; private set; }
		public int? TradelaneDown { get; private set; }
		public float? Damage { get; private set; }
		public List<List<string>> MissionType { get; private set; }
		public float? Sort { get; private set; }
		public string VignetteType { get; private set; }
		public int? Toughness { get; private set; }
		public int? Density { get; private set; }
		public bool? PopulationAdditive { get; private set; }
		public IValue ZoneCreationDistance { get; private set; }
		public int? RepopTime { get; private set; }
		public int? MaxBattleSize { get; private set; }
		public List<string> PopType { get; private set; }
		public int? ReliefTime { get; private set; }
		public List<IValue> PathLabel { get; private set; }
		public List<string> Usage { get; private set; }
		public bool? MissionEligible { get; private set; }
		public Dictionary<string, int> FactionWeight { get; private set; }
		public Dictionary<string, int> DensityRestriction { get; private set; }
		public List<Encounter> Encounters { get; private set; }

		public Zone(Section section, FreelancerData data)
			: base(section, data)
		{
			if (section == null) throw new ArgumentNullException("section");

			MissionType = new List<List<string>>();
			FactionWeight = new Dictionary<string, int>();
			DensityRestriction = new Dictionary<string, int>();
			Encounters = new List<Encounter>();

			foreach (Entry e in section)
			{
				if (!parentEntry(e))
					switch (e.Name.ToLowerInvariant())
				{
				case "shape":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (Shape != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					switch (e[0].ToString().ToLowerInvariant())
					{
					case "sphere":
						Shape = ZoneShape.SPHERE;
						break;
					case "ellipsoid":
						Shape = ZoneShape.ELLIPSOID;
						break;
					case "box":
						Shape = ZoneShape.BOX;
						break;
					case "cylinder":
						Shape = ZoneShape.CYLINDER;
						break;
					case "ring":
						Shape = ZoneShape.RING;
						break;
					}
					break;
				case "attack_ids":
					if (AttackIds != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					AttackIds = new List<IValue>();
					foreach (IValue i in e) AttackIds.Add(i);
					break;
				case "tradelane_attack":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (TradelaneAttack != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					TradelaneAttack = e[0].ToInt32();
					break;
				case "property_flags":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (PropertyFlags != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					PropertyFlags = e[0].ToInt32();
					break;
				case "property_fog_color":
					if (e.Count != 3) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (PropertyFogColor != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					PropertyFogColor = new Color4(e[0].ToInt32() / 255f, e[1].ToInt32() / 255f, e[2].ToInt32() / 255f, 1f);
					break;
				case "music":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (Music != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					Music = e[0].ToString();
					break;
				case "edge_fraction":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (EdgeFraction != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					EdgeFraction = e[0].ToSingle();
					break;
				case "spacedust":
				case "pacedust":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (Spacedust != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					Spacedust = e[0].ToString();
					break;
				case "spacedust_maxparticles":
				case "spacedusr_maxparticles":
				case "spacedust_masparticles":
				case "spacedust _maxparticles":
				case "spacedust_maxdust":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (SpacedustMaxparticles != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					SpacedustMaxparticles = e[0].ToInt32();
					break;
				case "interference":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (Interference != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					Interference = e[0].ToSingle();
					break;
				case "power_modifier":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (PowerModifier != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					PowerModifier = e[0].ToSingle();
					break;
				case "drag_modifier":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (DragModifier != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					DragModifier = e[0].ToSingle();
					break;
				case "comment":
					if (Comment != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					Comment = new List<string>();
					foreach (IValue i in e) Comment.Add(i.ToString());
					break;
				case "lane_id":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (LaneId != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					LaneId = e[0].ToInt32();
					break;
				case "tradelane_down":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (TradelaneDown != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					TradelaneDown = e[0].ToInt32();
					break;
				case "damage":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (Damage != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					Damage = e[0].ToSingle();
					break;
				case "mission_type":
					MissionType.Add(new List<string>());
					foreach (IValue i in e) MissionType[MissionType.Count - 1].Add(i.ToString());
					break;
				case "sort":
					if (e.Count != 1) FLLog.Warning("Ini", "Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (Sort != null) FLLog.Warning("Zone", "Duplicate " + e.Name + " Entry in " + section.Name);
					Sort = e[0].ToSingle();
					break;
				case "vignette_type":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (VignetteType != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					VignetteType = e[0].ToString();
					break;
				case "toughness":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (Toughness != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					Toughness = e[0].ToInt32();
					break;
				case "density":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (Density != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					Density = e[0].ToInt32();
					break;
				case "population_additive":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (PopulationAdditive != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					PopulationAdditive = e[0].ToBoolean();
					break;
				case "zone_creation_distance":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (ZoneCreationDistance != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					ZoneCreationDistance = e[0];
					break;
				case "repop_time":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (RepopTime != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					RepopTime = e[0].ToInt32(); ;
					break;
				case "max_battle_size":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (MaxBattleSize != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					MaxBattleSize = e[0].ToInt32();
					break;
				case "pop_type":
					if (PopType != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					PopType = new List<string>();
					foreach (IValue i in e) PopType.Add(i.ToString());
					break;
				case "relief_time":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (ReliefTime != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					ReliefTime = e[0].ToInt32();
					break;
				case "path_label":
					if (PathLabel != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					PathLabel = e.ToList<IValue>();
					break;
				case "usage":
					if (Usage != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					Usage = new List<string>();
					foreach (IValue i in e) Usage.Add(i.ToString());
					break;
				case "mission_eligible":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (MissionEligible != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					MissionEligible = e[0].ToBoolean();
					break;
				case "faction_weight":
					if (e.Count != 2) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (FactionWeight.ContainsKey(e[0].ToString())) {
						FLLog.Error("Ini", string.Format("Duplicate faction_weight for {0} in {1}, ignoring.", e[0], Nickname ?? "[undefined]"));
					} else {
						FactionWeight.Add(e[0].ToString(), e[1].ToInt32());
					}
					break;
				case "density_restriction":
					if (e.Count != 2) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (DensityRestriction.ContainsKey(e[1].ToString())) {
						FLLog.Error("Ini", string.Format("Duplicate density_restriction for {0} in {1}, ignoring.", e[1], Nickname ?? "[undefined]"));
					} else {
						DensityRestriction.Add(e[1].ToString(), e[0].ToInt32());
					}
					break;
				case "encounter":
					//if (e.Count != 3) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					//Encounters.Add(new Encounter(e[0].ToString(), e[1].ToInt32(), e[2].ToSingle()));
					break;
				case "faction":
					//TODO: Re-enable encounter parsing
					//if (Encounters.Count == 0) throw new Exception(e.Name + " before encounter");
					//if (e.Count == 1) {
						//Encounters[Encounters.Count - 1].Factions.Add(e[0].ToString(), 100);
					//}
					//else if (e.Count != 2) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					//else Encounters[Encounters.Count - 1].Factions.Add(e[0].ToString(), e[1].ToSingle());
					break;
				default:
					//throw new Exception("Invalid Entry in " + section.Name + ": " + e.Name);
						FLLog.Warning ("Zone", "Invalid Entry in " + section.Name + ": " + e.Name);
						break;
				}
			}
		}
	}
}