// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using LibreLancer.Ini;
using System.Collections.Generic;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Missions
{
    public class SpecificNPCIni : IniFile
    {
        [Section("NPC")] public List<SpecificNPC> Npcs = new List<SpecificNPC>();
        public void AddFile(string file, FileSystem vfs) => ParseAndFill(file, vfs);
    }

    public class SpecificNPC
    {
        [Entry("nickname")] public string Nickname;
        [Entry("base_appr")] public string BaseAppr;
        [Entry("individual_name")] public int IndividualName;
        [Entry("affiliation")] public string Affiliation;
        [Entry("voice")] public string Voice;
    }
}