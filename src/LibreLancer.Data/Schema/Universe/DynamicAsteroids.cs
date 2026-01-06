// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Universe;

[ParsedSection]
public partial class DynamicAsteroids
{
    [Entry("asteroid", Required = true)]
    public string Asteroid = null!;
    [Entry("count")]
    public int Count;
    [Entry("placement_radius")]
    public int PlacementRadius;
    [Entry("placement_offset")]
    public int PlacementOffset;
    [Entry("max_velocity")]
    public int MaxVelocity;
    [Entry("max_angular_velocity")]
    public int MaxAngularVelocity;
    [Entry("color_shift")]
    public Vector3 ColorShift;
}
