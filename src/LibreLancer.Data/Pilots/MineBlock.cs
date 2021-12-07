// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Ini;

namespace LibreLancer.Data.Pilots
{
    public class MineBlock : PilotBlock
    {
        [Entry("mine_launch_interval")] public float LaunchInterval;
        [Entry("mine_launch_cone_angle")] public float LaunchConeAngle;
        [Entry("mine_launch_range")] public float LaunchRange;
    }
}