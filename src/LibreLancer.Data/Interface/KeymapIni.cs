using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.IO;
using LibreLancer.Ini;

namespace LibreLancer.Data.Interface;

public class KeyCmd
{
    [Entry("nickname")]
    public string Nickname;
    [Entry("ids_name")] public int IdsName;
    [Entry("ids_info")] public int IdsInfo;
    [Entry("state")] public string[] State;
    [Entry("key")] public List<string[]> Keys = new List<string[]>();

    [EntryHandler("key", Multiline = true)]
    void HandleKey(Entry e) => Keys.Add(e.Select(x => x.ToString()).ToArray());
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