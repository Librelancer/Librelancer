// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Effects;

[ParsedSection]
public partial class VisEffect
{
    [Entry("nickname", Required = true)]
    public string Nickname = null!;
    [Entry("effect_crc")]
    public int EffectCrc;
    [Entry("alchemy", Required = true)]
    public string AlchemyPath = null!;
    [Entry("textures", Multiline = true)]
    public List<string> Textures = [];
}
