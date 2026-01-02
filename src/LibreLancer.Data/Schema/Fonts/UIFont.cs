// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Fonts;

[ParsedSection]
public partial class UIFont
{
    [Entry("nickname", Required = true)]
    public string Nickname = null!;
    [Entry("font", Required = true)]
    public string Font = null!;
    [Entry("fixed_height")]
    public float FixedHeight;
}
