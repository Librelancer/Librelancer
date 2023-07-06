// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.Solar;
using LibreLancer.GameData.World;
using DockSphere = LibreLancer.GameData.World.DockSphere;

namespace LibreLancer.GameData
{
    public class Archetype : IdentifiableItem
    {
        public ResolvedModel ModelFile;
        public string NavmapIcon;
        public ObjectLoadout Loadout;
        public Data.Solar.ArchetypeType Type;
        public List<DockSphere> DockSpheres = new List<DockSphere>();
        public float[] LODRanges;
        public Data.Solar.CollisionGroup[] CollisionGroups;
        public float SolarRadius;
        
        public bool IsUpdatableSolar()
        {
            switch (Type)
            {
                case ArchetypeType.airlock_gate:
                case ArchetypeType.destroyable_depot:
                case ArchetypeType.docking_ring:
                case ArchetypeType.jump_gate:
                case ArchetypeType.mission_satellite:
                case ArchetypeType.weapons_platform:
                case ArchetypeType.station:
                case ArchetypeType.satellite:
                case ArchetypeType.tradelane_ring:
                    return true;
                case ArchetypeType.non_targetable:
                case ArchetypeType.planet:
                case ArchetypeType.sun:
                case ArchetypeType.waypoint:
                default:
                    return false;
            }
        }
        
        public Archetype ()
        {
        }
    }
}