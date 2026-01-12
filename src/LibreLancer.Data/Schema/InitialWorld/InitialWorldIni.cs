using System.Collections.Generic;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.InitialWorld;

[ParsedIni]
public partial class InitialWorldIni
{
    [Section("locked_gates")]
    public List<LockedGates> LockedGates = [];

    [Section("group")]
    public List<FlGroup> Groups = [];

    public void AddFile(string path, FileSystem vfs, IniStringPool? stringPool = null) => ParseIni(path, vfs, stringPool);
}
