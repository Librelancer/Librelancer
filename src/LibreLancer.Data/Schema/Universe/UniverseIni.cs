// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Universe;

public class UniverseIni
{
    public List<Base> Bases = [];
    public List<StarSystem> Systems = [];

    public UniverseIni(string path, FreelancerData data, IniStringPool? stringPool = null)
    {
        var props = new IniParseProperties([
            new("dataPath", data.Freelancer.DataPath),
            new("universePath", data.VFS.RemovePathComponent(path)),
            new("vfs", data.VFS)
        ]);
        ParseIni(path, data.VFS, stringPool, props);
    }

    // Special case for parallel loading
    private void ParseIni(string path, FileSystem vfs, IniStringPool? stringPool, IniParseProperties properties)
    {
        using var stream = vfs.Open(path);
        List<Task<Base?>> baseTasks = [];
        List<Task<StarSystem?>> systemTasks = [];
        foreach (var section in IniFile.ParseFile(path, vfs, true))
        {
            var hash = ParseHelpers.Hash(section.Name);
            switch (hash)
            {
                case 0x5D3C9BE4: break; //ignore
                case 0x3DDC94D8:
                    if ("base".Equals(section.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        baseTasks.Add(Task.Run(() =>
                        {
                            Base.TryParse(section, out var val, stringPool, properties);
                            return val;
                        }));
                    }

                    break;
                case 0x491E0A9C:
                    if ("system".Equals(section.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        systemTasks.Add(Task.Run(() =>
                        {
                            StarSystem.TryParse(section, out var val, stringPool, properties);
                            return val;
                        }));
                    }
                    break;
                default:
                    IniDiagnostic.UnknownSection(section);
                    break;
            }
        }

        var a = Task.WhenAll(baseTasks);
        var b = Task.WhenAll(systemTasks);
        Task.WaitAll(new Task[] { a, b });

        Bases = a.Result.Where(x => x != null).ToList()!;
        Systems = b.Result.Where(x => x != null).ToList()!;
    }

}
