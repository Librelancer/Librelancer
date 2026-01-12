// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;

namespace LibreLancer.Data.Ini;

//Avoid repeated allocations in BINI loading
public class BiniStringBlock
{
    private readonly string block;
    private readonly Dictionary<int,string> strings = new();
    private readonly IniStringPool? stringPool;

    public BiniStringBlock(string block, IniStringPool? stringPool = null)
    {
        this.block = block;
        this.stringPool = stringPool;
    }

    public string Get(int strOffset)
    {
        if (strings.TryGetValue(strOffset, out var s))
        {
            return s;
        }

        s = stringPool != null
            ? stringPool.FromSpan(block.AsSpan(strOffset, block.IndexOf('\0', strOffset) - strOffset))
            : block.Substring(strOffset, block.IndexOf('\0', strOffset) - strOffset);

        strings.Add(strOffset, s);
        return s;
    }
}
