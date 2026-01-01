using System.Collections.Generic;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Missions;

[ParsedIni]
public partial class FormationsIni
{
    [Section("Formation")] public List<FormationDef> Formations = [];

    public void AddFile(string filename, FileSystem vfs, IniStringPool? stringPool = null)
    {
        ParseIni(filename, vfs, stringPool);
    }

}
