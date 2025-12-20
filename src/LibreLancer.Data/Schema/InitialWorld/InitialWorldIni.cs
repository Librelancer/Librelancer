using System.Collections.Generic;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.InitialWorld
{
    [ParsedIni]
    public partial class InitialWorldIni
    {
        [Section("locked_gates")]
        public List<LockedGates> LockedGates = new List<LockedGates>();

        [Section("group")]
        public List<FlGroup> Groups = new List<FlGroup>();

        public void AddFile(string path, FileSystem vfs, IniStringPool stringPool = null) => ParseIni(path, vfs, stringPool);
    }
}
