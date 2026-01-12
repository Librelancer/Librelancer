// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Interface;

public class InfocardMapIni
{
    public Dictionary<int, int> Map = new();

    public void AddMap(string file, FileSystem vfs, IniStringPool? stringPool = null)
    {
        foreach (var s in IniFile.ParseFile(file, vfs, false, stringPool))
        {
            if (!s.Name.Equals("infocardmaptable", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            foreach (var e in s)
            {
                if (!e.Name.Equals("map", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (e.Count < 2)
                    continue;
                Map[e[0].ToInt32()] = e[1].ToInt32();
            }
        }
    }
}
