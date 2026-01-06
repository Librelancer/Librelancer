// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Solar;

[ParsedSection]
public partial class StarGlow
{
    [Entry("nickname", Required = true)]
    public string Nickname = null!;
    [Entry("shape")]
    public string? Shape;
    [Entry("scale")]
    public int Scale;
    [Entry("inner_color", FloatColor=true)]
    public Color3f InnerColor;
    [Entry("outer_color", FloatColor=true)]
    public Color3f OuterColor;
}
