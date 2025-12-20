using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;
using LibreLancer.Data.Schema.Universe;

namespace LibreLancer.ContentEdit;

public static class ShortestPathWriter
{
    public static ShortestPathIni LoadShortestPathsSorted(string path, FileSystem vfs)
    {
        var ini = new ShortestPathIni();
        if (!vfs.FileExists(path))
            return ini;
        ini.AddFile(path, vfs);
        foreach (var s in ini.SystemConnections) {
            s.Entries.Sort((x,y) => string.Compare(x.End, y.End, StringComparison.Ordinal));
        }
        ini.SystemConnections.Sort((x, y) => string.Compare(x.Entries[0].Start, y.Entries[0].Start, StringComparison.Ordinal));
        return ini;
    }


    public static bool PathInisEqual(ShortestPathIni first, ShortestPathIni second)
    {
        if (first.SystemConnections.Count != second.SystemConnections.Count) return false;
        for (int i = 0; i < first.SystemConnections.Count; i++)
        {
            if(first.SystemConnections[i].Entries.Count != second.SystemConnections[i].Entries.Count) return false;
            for (int j = 0; j < first.SystemConnections[i].Entries.Count; j++)
            {
                if(first.SystemConnections[i].Entries[j].Start != second.SystemConnections[i].Entries[j].Start ||
                   first.SystemConnections[i].Entries[j].End != second.SystemConnections[i].Entries[j].End ||
                   first.SystemConnections[i].Entries[j].Hops.Length != second.SystemConnections[i].Entries[j].Hops.Length)
                    return false;
                for (int k = 0; k < first.SystemConnections[i].Entries[j].Hops.Length; k++)
                {
                    if (first.SystemConnections[i].Entries[j].Hops[k] != second.SystemConnections[i].Entries[j].Hops[k])
                        return false;
                }
            }
        }
        return true;
    }

    public static List<Section> Serialize(ShortestPathIni paths)
    {
        var ib = new IniBuilder();
        foreach (var s in paths.SystemConnections)
        {
            var section = ib.Section("SystemConnections");
            foreach (var e in s.Entries)
            {
                var v = new string[2 + e.Hops.Length];
                v[0] = e.Start;
                v[1] = e.End;
                e.Hops.CopyTo(v, 2);
                section.Entry("Path", v);
            }
        }
        return ib.Sections;
    }

}
