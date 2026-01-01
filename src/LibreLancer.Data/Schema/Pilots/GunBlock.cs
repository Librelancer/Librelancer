// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Pilots;

[ParsedSection]
public partial class GunBlock : PilotBlock
{
    [Entry("gun_fire_interval_time")] public float FireIntervalTime;
    [Entry("gun_fire_interval_variance_percent")]
    public float FireIntervalVariancePercent;
    [Entry("gun_fire_burst_interval_time")]
    public float FireBurstIntervalTime;
    [Entry("gun_fire_burst_interval_variance_percent")]
    public float FireBurstIntervalVariancePercent;
    [Entry("gun_fire_no_burst_interval_time")]
    public float FireNoBurstIntervalTime;
    [Entry("gun_fire_accuracy_cone_angle")]
    public float FireAccuracyConeAngle;
    [Entry("gun_fire_accuracy_power")] public float FireAccuracyPower;
    [Entry("gun_range_threshold")] public float RangeThreshold;
    [Entry("gun_target_point_switch_time")]
    public float TargetPointSwitchTime;
    [Entry("fire_style")] public string? FireStyle;
    [Entry("auto_turret_interval_time")] public float AutoTurretIntervalTime;
    [Entry("auto_turret_burst_interval_time")]
    public float AutoTurretBurstIntervalTime;
    [Entry("auto_turret_no_burst_interval_time")]
    public float AutoTurretNoBurstIntervalTime;
    [Entry("auto_turret_burst_interval_variance_percent")]
    public float AutoTurretBurstIntervalVariancePercent;
    [Entry("gun_range_threshold_variance_percent")]
    public float RangeThresholdVariancePercent;
    [Entry("gun_fire_accuracy_power_npc")]
    public float FireAccuracyPowerNpc;
}
