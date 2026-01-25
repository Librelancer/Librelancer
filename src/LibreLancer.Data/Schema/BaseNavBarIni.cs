// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema;

public class BaseNavBarIni
{
    public Dictionary<string, string> Navbar = new();
    public BaseNavBarIni(string? dataPath, FileSystem vfs, IniStringPool? stringPool = null)
    {
        foreach (var section in IniFile.ParseFile(dataPath + @"INTERFACE\BASESIDE\navbar.ini", vfs, true, stringPool))
        {
            if (section.Name.ToLowerInvariant() != "navbar")
            {
                continue;
            }

            foreach (var entry in section)
            {
                if (entry.Count is 0)
                {
                    FLLog.Error("Ini", "Navbar entry does not have any ini values");
                    entry.Add(new StringValue(""));
                }

                Navbar.Add(entry.Name, entry[0].ToString());
            }
        }
    }
}
