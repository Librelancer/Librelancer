using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Interface;

public class RolloverIni
{
    public Dictionary<int, int> Map = new();

    public void AddFile(string path, FileSystem vfs)
    {
        foreach (var section in IniFile.ParseFile(path, vfs, false, null))
        {
            if (!section.Name.Equals("RolloverTable", StringComparison.OrdinalIgnoreCase))
            {
                IniDiagnostic.UnknownSection(section);
                continue;
            }
            foreach (var e in section)
            {
                if (!e.Name.Equals("map", StringComparison.OrdinalIgnoreCase))
                {
                    IniDiagnostic.UnknownEntry(e, section);
                    continue;
                }
                if (ParseHelpers.ComponentCheck(2, section, e))
                {
                    Map[e[0].ToInt32()] = e[1].ToInt32();
                }
            }
        }
    }
}
