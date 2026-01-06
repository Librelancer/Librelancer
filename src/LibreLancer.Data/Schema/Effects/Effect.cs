// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Effects;

[ParsedSection]
public partial class Effect
{
    [Entry("nickname", Required = true)]
    public string Nickname = null!;
    [Entry("vis_effect")]
    public string? VisEffect;
    [Entry("vis_beam")]
    public string? VisBeam;
    [Entry("vis_generic")]
    public string? VisGeneric;
    [Entry("snd_effect")]
    public string? SndEffect;
    [Entry("type")]
    public string? Type; //Valid?
    [Entry("effect_type")]
    public string? EffectType;
    [Entry("lgt_effect")]
    public string? LgtEffect;
    [Entry("lgt_range_scale")]
    public float LgtRangeScale;
    [Entry("lgt_radius")]
    public float LgtRadius;
}
