using System.Collections.Generic;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Missions;

[ParsedIni]
public partial class FactionPropIni
{
    [Section("FactionProps")]
    public List<FactionProps> FactionProps = [];

    public void AddFile(string path, FileSystem vfs, IniStringPool? stringPool = null) => ParseIni(path, vfs, stringPool);
}
