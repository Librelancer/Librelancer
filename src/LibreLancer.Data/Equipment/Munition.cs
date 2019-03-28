// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using LibreLancer.Ini;
namespace LibreLancer.Data.Equipment
{
    public class Munition : AbstractEquipment
    {
        [Entry("const_effect")]
        public string ConstEffect;
        [Entry("requires_ammo")]
        public bool RequiresAmmo;
        [Entry("hull_damage")]
        public float HullDamage;
        [Entry("energy_damage")]
        public float EnergyDamage;
        [Entry("force_gun_ori")]
        public bool ForceGunOri;
        [Entry("mass")]
        public float Mass;
        [Entry("volume")]
        public float Volume;
        [Entry("weapon_type")]
        public string WeaponType;
        [Entry("lifetime")]
        public float Lifetime;

    }
}
