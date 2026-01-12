// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Universe;

public abstract class UniverseElement
{
    [Entry("nickname", Required = true)]
    public string Nickname = null!;
    [Entry("strid_name")]
    public int IdsName;
    [Entry("name")]
    public string? Name;

    public override string? ToString()
    {
        return Nickname;
    }
}
