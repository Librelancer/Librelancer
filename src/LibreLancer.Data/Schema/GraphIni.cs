// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema;

public class GraphIni
{
    public List<FloatGraph> FloatGraphs = [];

    public FloatGraph? FindFloatGraph(string nickname)
    {
        var result = FloatGraphs.Where(s => string.Equals(s.Name, nickname, StringComparison.InvariantCultureIgnoreCase)).ToArray();
        return result.Count() == 1 ? result.First() : null;
    }

    public void AddGraphIni(string path, FileSystem vfs, IniStringPool? stringPool = null)
    {
        foreach (var section in IniFile.ParseFile(path, vfs, false, stringPool))
        {
            if (section.Name.ToLowerInvariant() != "igraph")
                throw new Exception("Unexpected section in Graph ini: " + section.Name);
            string? nickname = null;
            FloatGraph? fg = null;
            bool skip = false;
            foreach (var e in section)
            {
                if (skip)
                    break;
                switch (e.Name.ToLowerInvariant())
                {
                    case "nickname":
                        nickname = e[0].ToString();
                        break;
                    case "type":
                        var t = e[0].ToString().ToUpperInvariant();
                        if (t == "FLOAT")
                            fg = new FloatGraph();
                        else
                            skip = true;
                        break;
                    case "point":
                        if (fg == null)
                            throw new Exception("Point appearing after type");
                        fg.Points.Add(
                            new Vector2(
                                e[0].ToInt32(),
                                e[1].ToSingle()
                            )
                        );
                        break;
                }
            }
            if (skip)
                continue;

            fg!.Name = nickname!;
            FloatGraphs.Add(fg);
        }
    }
}
