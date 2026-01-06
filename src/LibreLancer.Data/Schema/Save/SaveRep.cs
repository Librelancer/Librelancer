// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Save;

public class SaveRep
{
    public float Reputation;
    public string? Group;

    public SaveRep() { }
    public SaveRep(Entry e)
    {
        Reputation = e[0].ToSingle();
        Group = e[1].ToString();
    }
}
