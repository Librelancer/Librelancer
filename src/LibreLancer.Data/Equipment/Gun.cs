// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Ini;
namespace LibreLancer.Data.Equipment
{
    public class Gun : AbstractEquipment
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
    }
}
