using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.IO;
using LibreLancer.Ini;

namespace LibreLancer.Data.Universe;

public class ShortestPathIni : IniFile
{
    [Section("SystemConnections")]
    public List<SystemConnections> SystemConnections = new();

    public void AddFile(string path, FileSystem vfs) => ParseAndFill(path, vfs);
}

public record ShortestPathEntry(string Start, string End, string[] Hops);

public class SystemConnections
{
    public List<ShortestPathEntry> Entries = new();

    [EntryHandler("Path", Multiline = true, MinComponents = 3)]
    public void HandlePathEntry(Entry e)
    {
        Entries.Add(new ShortestPathEntry(e[0].ToString(), e[1].ToString(),
            e.Skip(2).Select(x => x.ToString()).ToArray()));
    }
}
