using System.Collections.Generic;
using LibreLancer.Data.IO;
using LibreLancer.Ini;

namespace LibreLancer.Data.Missions
{
    public class EmpathyIni : IniFile
    {
        [Section("RepChangeEffects")] 
        public List<RepChangeEffects> RepChangeEffects = new List<RepChangeEffects>();

        public void AddFile(string path, FileSystem vfs) => ParseAndFill(path, vfs);
    }
}