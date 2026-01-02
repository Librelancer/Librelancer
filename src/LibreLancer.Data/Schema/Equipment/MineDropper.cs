// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Equipment;

[ParsedSection]
public partial class MineDropper : AbstractEquipment
{
    [Entry("projectile_archetype", Required = true)] public string ProjectileArchetype = null!;
    [Entry("dry_fire_sound")] public string? DryFireSound;
    [Entry("power_usage")] public float PowerUsage;
    [Entry("refire_delay")] public float RefireDelay;
    [Entry("muzzle_velocity")] public float MuzzleVelocity;
    [Entry("damage_per_fire")] public float DamagePerFire;
}
