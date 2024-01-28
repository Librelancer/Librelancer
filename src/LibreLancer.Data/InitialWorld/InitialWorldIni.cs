using System.Collections.Generic;
using LibreLancer.Data.IO;
using LibreLancer.Ini;

namespace LibreLancer.Data.InitialWorld
{
    [SelfSection("locked_gates")]
    public class InitialWorldIni : IniFile
    {
        [Entry("locked_gate", Multiline = true)]
        public List<int> LockedGates = new List<int>();

        [Entry("npc_locked_gate", Multiline = true)]
        public List<int> NpcLockedGates = new List<int>();

        [Section("group")]
        public List<FlGroup> Groups = new List<FlGroup>();

        public void AddFile(string path, FileSystem vfs) => ParseAndFill(path, vfs);
    }
}