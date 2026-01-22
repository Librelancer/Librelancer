using System.Collections.Generic;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Missions;

[ParsedIni]
public partial class EncounterIni
{
    [Section("EncounterFormation")]
    public List<EncounterFormation> Formations = new();
    [Section("Creation")]
    public EncounterPermutations? Permutations;

    public EncounterIni()
    {
    }

    public EncounterIni(string file, FileSystem? vfs)
    {
        ParseIni(file, vfs);
    }
}
