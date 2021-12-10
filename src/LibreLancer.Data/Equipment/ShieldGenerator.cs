// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System.Collections.Generic;
using LibreLancer.Ini;

namespace LibreLancer.Data.Equipment
{
    public class ShieldGenerator : AbstractEquipment, ICustomEntryHandler
    {
        [Entry("shield_rebuilt_sound")] public string ShieldRebuiltSound;
        [Entry("shield_collapse_particle")] public string ShieldCollapseParticle;
        [Entry("shield_collapse_sound")] public string ShieldCollapseSound;
        [Entry("offline_rebuild_time")] public float OfflineRebuildTime;
        [Entry("offline_threshold")] public float OfflineThreshold;
        [Entry("regeneration_rate")] public float RegenerationRate;
        [Entry("constant_power_draw")] public float ConstantPowerDraw;
        [Entry("rebuild_power_draw")] public float RebuildPowerDraw;
        [Entry("max_capacity")] public float MaxCapacity;
        [Entry("hp_type")] public string HpType;
        [Entry("shield_type")] public string ShieldType;
        public List<ShieldHitEffect> ShieldHitEffects = new List<ShieldHitEffect>();

        private static readonly CustomEntry[] _custom = new CustomEntry[]
        {
            new("shield_hit_effects", (s,e) => ((ShieldGenerator)s).HandleShieldHitEffects(e)),
        };

        IEnumerable<CustomEntry> ICustomEntryHandler.CustomEntries => _custom;
        
        void HandleShieldHitEffects(Entry e)
        {
            ShieldHitEffects.Add(new ShieldHitEffect()
            {
                Number = e[0].ToSingle(), Effect = e[1].ToString()
            });
        }
    }

    public class ShieldHitEffect
    {
        public float Number;
        public string Effect;
    }
}