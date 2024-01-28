using System.Collections.Generic;
using LibreLancer.Data.IO;
using LibreLancer.Ini;

namespace LibreLancer.Data.Missions;

public class FormationsIni : IniFile
{
    [Section("Formation")] public List<FormationDef> Formations = new List<FormationDef>();

    public void AddFile(string filename, FileSystem vfs)
    {
        ParseAndFill(filename, vfs);
    }

}
