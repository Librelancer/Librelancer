using System.Collections.Generic;
using LibreLancer.Data.IO;
using LibreLancer.Ini;

namespace LibreLancer.Data.Missions
{
    public class FactionPropIni : IniFile
    {
        [Section("FactionProps")] 
        public List<FactionProps> FactionProps = new List<FactionProps>();

        public void AddFile(string path, FileSystem vfs) => ParseAndFill(path, vfs);
    }
}