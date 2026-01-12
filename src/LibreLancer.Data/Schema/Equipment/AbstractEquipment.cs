// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Equipment;

[BaseSection]
public abstract partial class AbstractEquipment
{
    [Entry("nickname")] // Should be required, while LOD is AbstractEquipment it can't be. (Child section needs rework)
    public string Nickname = null!;
    [Entry("da_archetype")]
    public string? DaArchetype;
    [Entry("inherit")]
    public string? Inherit;
    [Entry("material_library", Multiline = true)]
    public List<string> MaterialLibrary = [];
    [Entry("lodranges")]
    public float[]? LODRanges;
    [Entry("hp_child")]
    public string? HPChild;
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
    public string? SeparationExplosion;
    [Entry("debris_type")]
    public string? DebrisType;
    [Entry("indestructible")]
    public bool Indestructible;
    [Entry("loot_appearance")]
    public string? LootAppearance;
    [Entry("units_per_container")]
    public int UnitsPerContainer;
    [Section("lod", Child = true)]
    public List<Lod> LodInfo = [];
}
