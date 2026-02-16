using System.Collections.Generic;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Missions;

[ParsedIni]
public partial class ShipClassesIni
{
    [Section("ShipClass")]
    public List<ShipClass> Classes = [];

    public void AddFile(string file, FileSystem vfs, IniStringPool? stringPool = null) => ParseIni(file, vfs, stringPool);
}

[ParsedSection]
public partial class ShipClass
{
    [Entry("nickname", Required = true)] public string Nickname = "";
    [Entry("member", Multiline = true)] public List<string> Members = [];
}
