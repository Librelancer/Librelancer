// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

using LibreLancer.Ini;

namespace LibreLancer.Data.Equipment
{
    public abstract class AbstractEquipment
    {
        [Entry("nickname")]
        public string Nickname;
        [Entry("da_archetype")]
        public string DaArchetype;
        [Entry("material_library")]
        public string MaterialLibrary;
        [Entry("lodranges")]
        public float[] LODRanges;
        [Entry("hp_child")]
        public string HPChild;
        [Entry("ids_name")]
        public int IdsName = -1;
        [Entry("ids_info")]
        public int IdsInfo = -1;
        [Entry("lootable")]
        public bool Lootable;
        [Entry("hit_pts")]
        public int Hitpoints;
        [Entry("mass")]
        public float Mass;
        [Entry("volume")]
        public float Volume;
        [Entry("parent_impulse")]
        public float ParentImpulse;
        [Entry("child_impulse")]
        public float ChildImpulse;
        [Entry("toughness")]
        public float Toughness;
        [Entry("explosion_resistance")]
        public float ExplosionResistance;
        [Entry("separation_explosion")]
        public string SeparationExplosion;
        [Entry("debris_type")]
        public string DebrisType;
        [Entry("indestructible")]
        public bool Indestructible;
    }
}
