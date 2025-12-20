// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Pilots
{
    [ParsedSection]
    public partial class TrailBlock : PilotBlock
    {
        [Entry("trail_lock_cone_angle")] public float LockConeAngle;
        [Entry("trail_break_time")] public float BreakTime;
        [Entry("trail_min_no_lock_time")] public float MinNoLockTime;
        [Entry("trail_break_roll_throttle")] public float BreakRollThrottle;
        [Entry("trail_break_afterburner")] public bool BreakAfterburner;
        [Entry("trail_max_turn_throttle")] public float MaxTurnThrottle;
        [Entry("trail_distance")] public float Distance;
    }
}
