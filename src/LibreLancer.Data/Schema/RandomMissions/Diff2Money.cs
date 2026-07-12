using System.Collections.Generic;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.RandomMissions;

[ParsedIni]
public partial class Diff2MoneyIni
{
    [Section("Diff2Money")]
    public Diff2Money? Graph;

    public void AddFile(string path, FileSystem vfs) => ParseIni(path, vfs);
}

[ParsedSection]
public partial class Diff2Money
{
    public List<(float Difficulty, float Money)> Graph = [];

    [EntryHandler("Diff2Money", Multiline = true, MinComponents = 2)]
    void HandleDiff2Money(Entry e) => Graph.Add((e[0].ToSingle(), e[1].ToSingle()));
}
