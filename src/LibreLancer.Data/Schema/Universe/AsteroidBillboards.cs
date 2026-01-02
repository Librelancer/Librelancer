// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Universe;

[ParsedSection]
public partial class AsteroidBillboards
{
    [Entry("count")]
    public int? Count;

    [Entry("start_dist")]
    public int? StartDist;

    [Entry("fade_dist_percent")]
    public float? FadeDistPercent;

    [Entry("shape")]
    public string? Shape;

    [Entry("color_shift")]
    public Vector3? ColorShift;

    [Entry("ambient_intensity")]
    public float? AmbientIntensity;

    [Entry("size")]
    public Vector2? Size;
}
