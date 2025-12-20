using System.Collections.Generic;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Missions
{
    [ParsedIni]
    public partial class EmpathyIni
    {
        [Section("RepChangeEffects")]
        public List<RepChangeEffects> RepChangeEffects = new List<RepChangeEffects>();

        public void AddFile(string path, FileSystem vfs, IniStringPool stringPool = null) => ParseIni(path, vfs, stringPool);
    }
}
