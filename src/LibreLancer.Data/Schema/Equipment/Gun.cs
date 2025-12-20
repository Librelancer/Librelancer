// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Equipment
{
    [ParsedSection]
    [BaseSection]
    public partial class Gun : AbstractEquipment
    {
        [Entry("turn_rate")]
        public float TurnRate;
        [Entry("muzzle_velocity")]
        public float MuzzleVelocity;
        [Entry("power_usage")]
        public float PowerUsage;
        [Entry("refire_delay")]
        public float RefireDelay;
        [Entry("projectile_archetype")]
        public string ProjectileArchetype;
        [Entry("flash_particle_name")]
        public string FlashParticleName;
        [Entry("flash_radius")]
        public float FlashRadius;
        [Entry("auto_turret")]
        public bool AutoTurret;
        [Entry("light_anim")]
        public string LightAnim;
        [Entry("damage_per_fire")]
        public float DamagePerFire;
        [Entry("use_animation")]
        public string UseAnimation;
        [Entry("hp_gun_type")]
        public string HpGunType;
        [Entry("dry_fire_sound")]
        public string DryFireSound;
        [Entry("force_gun_ori")]
        public bool ForceGunOri;
        [Entry("dispersion_angle")]
        public float DispersionAngle;
    }
}
