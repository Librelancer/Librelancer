// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Pilots;

public enum LootPreference
{
    LT_NONE,
    LT_POTIONS,
    LT_COMMODITIES,
    LT_ALL
}

public enum FieldTargeting
{
    Always,
    Never,
    Low_Density,
    High_Density
}


public enum DifficultyThreshold
{
    Easiest,
    Easy,
    Equal,
    Hard,
    Hardest
}

[ParsedSection]
public partial class JobBlock : PilotBlock
{
    [Entry("wait_for_leader_target")] public bool WaitForLeaderTarget;

    [Entry("flee_when_leader_flees_style")]
    public bool FleeWhenLeaderFleesStyle;

    [Entry("force_attack_formation")] public bool ForceAttackFormation;

    [Entry("maximum_leader_target_distance")]
    public float MaximumLeaderTargetDistance;

    [Entry("scene_toughness_threshold")] public DifficultyThreshold SceneToughnessThreshold;
    [Entry("flee_scene_threat_style")] public DifficultyThreshold FleeSceneThreatStyle;

    [Entry("flee_when_hull_damaged_percent")]
    public float FleeWhenHullDamagedPercent;

    [Entry("flee_no_weapons_style")] public bool FleeNoWeaponsStyle;
    [Entry("loot_flee_threshold")] public DifficultyThreshold LootFleeThreshold;
    [Entry("attack_subtarget_order")] public string? AttackSubtargetOrder;
    [Entry("field_targeting")] public FieldTargeting FieldTargeting;
    [Entry("loot_preference")] public LootPreference LootPreference;
    [Entry("combat_drift_distance")] public float CombatDriftDistance;
    [Entry("allow_player_targeting")] public bool AllowPlayerTargeting;

    public List<AttackPreference> AttackPreferences = [];

    [EntryHandler("attack_preference", MinComponents = 3, Multiline = true)]
    private void HandleAttackPreference(Entry e) => AttackPreferences.Add(new AttackPreference(e));

}

public class AttackPreference
{
    public AttackTarget Target;
    public float Unknown;
    public AttackFlags Flags;

    public AttackPreference()
    {
    }

    public AttackPreference(Entry e)
    {
        Target = Enum.Parse<AttackTarget>(e[0].ToString(), true);
        Unknown = e[1].ToSingle();
        var flags = e[2].ToString()
            .Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        foreach (var f in flags)
            Flags |= Enum.Parse<AttackFlags>(f, true);
    }
}

public enum AttackTarget
{
    Fighter,
    Freighter,
    Transport,
    Gunboat,
    Cruiser,
    Capital,
    Jumpgate,
    Weapons_Platform,
    Solar,
    Destroyable_Depot,
    Tradelane,
    Anything
}

[Flags]
public enum AttackFlags
{
    None = 0,
    Guns = 1 << 1,
    Guided = 1 << 2,
    Unguided = 1 << 3,
    Torpedo = 1 << 4
}
