using System;
using LibreLancer.Ini;
namespace LibreLancer.Data.Equipment
{
    public class Munition
    {
        [Entry("nickname")]
        public string Nickname;
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
        [Entry("hit_pts")]
        public float Hitpoints;
        [Entry("weapon_type")]
        public string WeaponType;
        [Entry("lifetime")]
        public float Lifetime;

    }
}
