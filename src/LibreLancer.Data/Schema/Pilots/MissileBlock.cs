// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Pilots;

[ParsedSection]
public partial class MissileBlock : PilotBlock
{
    [Entry("missile_launch_interval_time")]
    public float LaunchIntervalTime;
    [Entry("missile_launch_interval_variance_percent")]
    public float LaunchVariancePercent;
    [Entry("missile_launch_range")] public float LaunchRange;
    [Entry("missile_launch_cone_angle")] public float LaunchConeAngle;
    [Entry("missile_launch_allow_out_of_range")] public bool MissileLaunchAllowOutOfRange;
}