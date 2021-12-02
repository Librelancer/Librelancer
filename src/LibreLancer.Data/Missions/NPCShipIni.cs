// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System.Collections.Generic;
using LibreLancer.Ini;

namespace LibreLancer.Data.Missions
{
    public class NPCShipIni : IniFile
    {
        [Section("NPCShipArch")] 
        public List<NPCShipArch> ShipArches = new List<NPCShipArch>();
        public NPCShipIni(string path, FileSystem vfs)
        {
            ParseAndFill(path, vfs);
        }
    }

    public class NPCShipArch
    {
        [Entry("nickname")] public string Nickname;
        [Entry("loadout")] public string Loadout;
        [Entry("level")] public string Level;
        [Entry("ship_archetype")] public string ShipArchetype;
        [Entry("pilot")] public string Pilot;
        [Entry("state_graph")] public string StateGraph;
        [Entry("npc_class")] public List<string> NpcClass = new List<string>();
    }
}