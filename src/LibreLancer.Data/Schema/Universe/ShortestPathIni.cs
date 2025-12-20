using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Universe;

[ParsedIni]
public partial class ShortestPathIni
{
    [Section("SystemConnections")]
    public List<SystemConnections> SystemConnections = new();

    public void AddFile(string path, FileSystem vfs) => ParseIni(path, vfs);
}

public record ShortestPathEntry(string Start, string End, string[] Hops);

[ParsedSection]
public partial class SystemConnections
{
    public List<ShortestPathEntry> Entries = new();

    [EntryHandler("Path", Multiline = true, MinComponents = 3)]
    public void HandlePathEntry(Entry e)
    {
        Entries.Add(new ShortestPathEntry(e[0].ToString(), e[1].ToString(),
            e.Skip(2).Select(x => x.ToString()).ToArray()));
    }
}
