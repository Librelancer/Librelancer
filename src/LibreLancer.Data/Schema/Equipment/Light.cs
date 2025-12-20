// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Equipment
{
    [ParsedSection]
    public partial class Light : AbstractEquipment
    {
        [Entry("always_on")] public bool? AlwaysOn;
        [Entry("docking_light")] public bool? DockingLight;
        [Entry("bulb_size")] public float? BulbSize;
        [Entry("glow_size")] public float? GlowSize;
        [Entry("glow_color")] public Color3f? GlowColor;
        [Entry("color")] public Color3f? Color;
        [Entry("flare_cone")] public Vector2? FlareCone;
        [Entry("intensity")] public int? Intensity;
        [Entry("lightsource_cone")] public int? LightSourceCone;
        [Entry("min_color")] public Color3f? MinColor;
        [Entry("avg_delay")] public float? AvgDelay;
        [Entry("blink_duration")] public float? BlinkDuration;
        [Entry("emit_range")] public float? EmitRange;
        [Entry("emit_attenuation")] public Vector3? EmitAttenuation;
        [Entry("shape")] public string Shape;
    }
}
