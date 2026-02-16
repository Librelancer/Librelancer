// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Universe;

[ParsedSection]
public partial class Zone : SystemPart
{
    [Entry("shape")]
    public ZoneShape? Shape;

    [Entry("attack_ids")]
    public string[] AttackIds = [];

    [Entry("tradelane_attack")]
    public int TradelaneAttack;

    [Entry("property_flags")]
    public int PropertyFlags;

    [Entry("property_fog_color")]
    public Color4? PropertyFogColor;

    [Entry("music")]
    public string? Music;

    [Entry("edge_fraction")]
    public float? EdgeFraction;

    [Entry("spacedust")]
    public string? Spacedust;

    [Entry("spacedust_maxparticles")]
    public int SpacedustMaxParticles;

    [Entry("interference")]
    public float Interference;

    [Entry("power_modifier")]
    public float PowerModifier;

    [Entry("drag_modifier")]
    public float DragModifier;

    [Entry("comment")]
    public string? Comment;

    [Entry("lane_id")]
    public int LaneId;

    [Entry("tradelane_down")]
    public int TradelaneDown;

    [Entry("damage")]
    public float Damage;

    [Entry("mission_type")]
    public string[] MissionType = [];

    [Entry("sort")]
    public float? Sort;

    [Entry("vignette_type")]
    public string? VignetteType;

    [Entry("toughness")]
    public int Toughness;

    [Entry("density")]
    public int Density;

    [Entry("population_additive")]
    public bool? PopulationAdditive;

    [Entry("zone_creation_distance")]
    public string? ZoneCreationDistance;

    [Entry("repop_time")]
    public int RepopTime;

    [Entry("max_battle_size")]
    public int MaxBattleSize;

    [Entry("pop_type")]
    public string[] PopType = [];

    [Entry("relief_time")]
    public int ReliefTime;

    [Entry("path_label")]
    public string[] PathLabel = [];

    [Entry("usage")]
    public string[] Usage = [];

    [Entry("mission_eligible")]
    public bool MissionEligible;

    public List<Encounter> Encounters { get; private set; } = [];
    public List<DensityRestriction> DensityRestrictions { get; private set; } = [];

    [EntryHandler("encounter", MinComponents = 1, Multiline = true)]
    private void HandleEncounter(Entry e) => Encounters.Add(new Encounter(e));

    [EntryHandler("faction", MinComponents = 2, Multiline = true)]
    [EntryHandler("faction_weight", MinComponents = 2, Multiline = true)]
    private void HandleFaction(Entry e)
    {
        if (Encounters.Count == 0) {
            //FLLog.Warning("Ini", $"faction entry without matching encounter at {e.Section.File}:{e.Line}");
        }
        else {
            Encounters[^1].FactionSpawns.Add(new (e[0].ToString(), e[1].ToSingle()));
        }
    }

    [EntryHandler("density_restriction", MinComponents = 2, Multiline = true)]
    private void HandleDensityRestriction(Entry e) => DensityRestrictions.Add(new DensityRestriction(e[0].ToInt32(), e[1].ToString()));
}
