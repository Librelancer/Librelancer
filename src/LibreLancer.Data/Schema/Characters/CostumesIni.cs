// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Characters;

[ParsedIni]
public partial class CostumesIni
{
    [Section("costume")] public List<Costume> Costumes = [];

    public CostumesIni(string path, FileSystem vfs, IniStringPool? stringPool = null)
    {
        ParseIni(path!, vfs, stringPool);
    }

    public Costume? FindCostume(string nickname)
    {
        var candidates = Costumes
            .Where(c => c.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase)).ToArray();
        var count = candidates.Count<Costume>();
        return count switch
        {
            1 => candidates.First<Costume>(),
            0 => null,
            _ => throw new Exception(count + " Costumes with nickname " + nickname)
        };
    }
}
