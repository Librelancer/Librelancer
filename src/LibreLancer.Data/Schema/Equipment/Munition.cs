// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Equipment;

[ParsedSection]
[BaseSection]
public partial class Munition : AbstractEquipment
{
    [Entry("const_effect")]
    public string? ConstEffect;
    [Entry("requires_ammo")]
    public bool RequiresAmmo;
    [Entry("hull_damage")]
    public float HullDamage;
    [Entry("energy_damage")]
    public float EnergyDamage;
    [Entry("force_gun_ori")]
    public bool ForceGunOri;
    [Entry("weapon_type")]
    public string? WeaponType;
    [Entry("lifetime")]
    public float Lifetime;
    [Entry("one_shot_sound")]
    public string? OneShotSound;
    [Entry("detonation_dist")]
    public float DetonationDist;
    [Entry("motor")]
    public string? Motor;
    [Entry("seeker")]
    public string? Seeker;
    [Entry("seeker_range")]
    public float SeekerRange;
    [Entry("seeker_fov_deg")]
    public float SeekerFovDeg;
    [Entry("max_angular_velocity")]
    public float MaxAngularVelocity;
    [Entry("time_to_lock")]
    public float TimeToLock;
    [Entry("hp_trail_parent")]
    public string? HpTrailParent;
    [Entry("hp_type")]
    public string? HpType;
    [Entry("explosion_arch")]
    public string? ExplosionArch;
    [Entry("munition_hit_effect")]
    public string? MunitionHitEffect;
    [Entry("cruise_disruptor")]
    public bool CruiseDisruptor;
}
