// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Missions
{
    [ParsedIni]
    public partial class SpecificNPCIni
    {
        [Section("NPC")] public List<SpecificNPC> Npcs = new List<SpecificNPC>();
        public void AddFile(string file, FileSystem vfs, IniStringPool stringPool = null) => ParseIni(file, vfs, stringPool);
    }

    [ParsedSection]
    public partial class SpecificNPC
    {
        [Entry("nickname")] public string Nickname;
        [Entry("base_appr")] public string BaseAppr;
        [Entry("individual_name")] public int IndividualName;
        [Entry("affiliation")] public string Affiliation;
        [Entry("voice")] public string Voice;
    }
}
