// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Pilots
{
    [ParsedSection]
    public partial class RepairBlock : PilotBlock
    {
        [Entry("use_shield_repair_pre_delay")] public float UseShieldRepairPreDelay;
        [Entry("use_shield_repair_post_delay")]
        public float UseShieldRepairPostDelay;
        [Entry("use_shield_repair_at_damage_percent")]
        public float UseShieldRepairAtDamagePercent;
        [Entry("use_hull_repair_pre_delay")] public float UseHullRepairPreDelay;
        [Entry("use_hull_repair_post_delay")] public float UseHullRepairPostDelay;
        [Entry("use_hull_repair_at_damage_percent")]
        public float UseHullRepairAtDamagePercent;
    }
}
