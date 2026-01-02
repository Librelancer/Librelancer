// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Numerics;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Effects;

[ParsedSection]
public partial class EffectType
{
    [Entry("nickname", Required = true)]
    public string Nickname = null!;
    [Entry("priority")]
    public float Priority;
    [Entry("generic_priority")]
    public float GenericPriority;
    [Entry("lod_type")]
    public string? LodType;
    [Entry("radius")]
    public float Radius;
    [Entry("visibility")]
    public string? Visibility;
    [Entry("update")]
    public string? Update;
    [Entry("run_time")]
    public float RunTime;
    [Entry("pbubble")]
    public Vector2 Pbubble;
}
