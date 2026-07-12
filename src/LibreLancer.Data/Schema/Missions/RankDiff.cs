using System.Collections.Generic;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Missions;

[ParsedIni]
public partial class RankDiffIni
{
    [Section("RankDiffDB")]
    public RankDiffDB? Graph;

    public void AddFile(string path, FileSystem vfs) => ParseIni(path, vfs);
}

[ParsedSection]
public partial class RankDiffDB
{
    public List<(string Rank, float Difficulty)> Graph = [];

    [EntryHandler("rank_diff", Multiline = true, MinComponents = 2)]
    void HandleDiff2Money(Entry e) => Graph.Add((e[0].ToString(), e[1].ToSingle()));
}
