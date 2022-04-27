using System.Collections.Generic;
using System.Linq;
using LibreLancer.Ini;

namespace LibreLancer.Data.Interface;

public class KeyCmd : ICustomEntryHandler
{
    [Entry("nickname")]
    public string Nickname;
    [Entry("ids_name")] public int IdsName;
    [Entry("ids_info")] public int IdsInfo;
    [Entry("state")] public string[] State;
    [Entry("key")] public List<string[]> Keys = new List<string[]>();
    
    private static readonly CustomEntry[] _custom = new CustomEntry[]
    {
        new("key", (s,e) => ((KeyCmd)s).Keys.Add(e.Select(x => x.ToString()).ToArray()))
    };

    IEnumerable<CustomEntry> ICustomEntryHandler.CustomEntries => _custom;
}

[IgnoreSection("keymap=1.1")]
[IgnoreSection("KeyMap")]
public class KeymapIni : IniFile
{
    [Section("KeyCmd")]
    public List<KeyCmd> KeyCmd = new List<KeyCmd>();
    
    public KeymapIni(string path, FileSystem vfs)
    {
        ParseAndFill(path, vfs);
    }
}