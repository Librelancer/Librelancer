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
        [Entry("shape")] 
        public ZoneShape? Shape;
        
        [Entry("attack_ids")]
        public string[] AttackIds;
        
        [Entry("tradelane_attack")] 
        public int? TradelaneAttack;
        
        [Entry("property_flags")] 
        public int? PropertyFlags;

        [Entry("property_fog_color")] 
        public Color4? PropertyFogColor;

        [Entry("music")] 
        public string Music;

        [Entry("edge_fraction")] 
        public float? EdgeFraction;

        [Entry("spacedust")] 
        public string Spacedust;

        [Entry("spacedust_maxparticles")] 
        public int? SpacedustMaxParticles;

        [Entry("interference")] 
        public float? Interference;

        [Entry("powermodifier")] 
        public float? PowerModifier;

        [Entry("dragmodifier")] 
        public float? DragModifier;

        [Entry("comment")] 
        public string[] Comment;

        [Entry("lane_id")] 
        public int? LaneId;

        [Entry("tradelane_down")] 
        public int? TradelaneDown;

        [Entry("damage")]
        public float? Damage;

        [Entry("mission_type")]
        public List<string[]> MissionType = new List<string[]>();

        [Entry("sort")] 
        public float? Sort;

        [Entry("vignette_type")] 
        public string VignetteType;

        [Entry("toughness")] 
        public int? Toughness;

        [Entry("density")] 
        public int? Density;

        [Entry("population_additive")] 
        public bool? PopulationAdditive;

        [Entry("zone_creation_distance")] 
        public string ZoneCreationDistance;

        [Entry("repop_time")] 
        public int? RepopTime;
        
        [Entry("max_battle_size")] 
        public int? MaxBattleSize;
        
        [Entry("pop_type")]
        public string[] PopType;

        [Entry("relief_time")] 
        public int? ReliefTime;
        
        [Entry("path_label")]
        public string[] PathLabel;
        
        [Entry("usage")] 
        public string[] Usage;

        [Entry("mission_eligible")] 
        public bool? MissionEligible;

        
		//public Dictionary<string, int> FactionWeight { get; private set; }
		//public Dictionary<string, int> DensityRestriction { get; private set; }
        
		//public List<Encounter> Encounters { get; private set; }

        bool HandleEntry(Entry e)
        {
            if (e.Name.Equals("encounter", StringComparison.OrdinalIgnoreCase) ||
                e.Name.Equals("faction", StringComparison.OrdinalIgnoreCase) ||
                e.Name.Equals("faction_weight", StringComparison.OrdinalIgnoreCase) ||
                e.Name.Equals("density_restriction", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }
    }
}