// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Schema.Solar;
using DockSphere = LibreLancer.Data.GameData.World.DockSphere;

namespace LibreLancer.Data.GameData
{
    public class Archetype : IdentifiableItem
    {
        public ResolvedModel ModelFile;
        public string NavmapIcon;
        public ObjectLoadout Loadout;
        public ArchetypeType Type;
        public List<DockSphere> DockSpheres = new List<DockSphere>();
        public float[] LODRanges;
        public List<SeparablePart> SeparableParts = new List<SeparablePart>();
        public float SolarRadius;
        public float Hitpoints;

        public bool CanVisit => Type switch
        {
            ArchetypeType.docking_ring => true,
            ArchetypeType.jump_gate => true,
            ArchetypeType.jump_hole => true,
            ArchetypeType.planet => true,
            ArchetypeType.satellite => true,
            ArchetypeType.station => true,
            ArchetypeType.sun => true,
            ArchetypeType.weapons_platform => true,
            _ => false
        };

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
