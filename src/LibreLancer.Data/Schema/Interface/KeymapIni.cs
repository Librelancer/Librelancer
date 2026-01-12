using System.Collections.Generic;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Interface;

[ParsedSection]
public partial class KeyCmd
{
    [Entry("nickname", Required = true)] public string Nickname = null!;
    [Entry("ids_name")] public int IdsName;
    [Entry("ids_info")] public int IdsInfo;
    [Entry("state")] public string[] State = [];
    [Entry("key")] public List<string[]> Keys = [];
}

[IgnoreSection("keymap=1.1")]
[IgnoreSection("KeyMap")]
[ParsedIni]
public partial class KeymapIni
{
    [Section("KeyCmd")]
    public List<KeyCmd> KeyCmd = [];

    public KeymapIni(string path, FileSystem vfs, IniStringPool? stringPool = null)
    {
        ParseIni(path, vfs, stringPool);
    }
}
