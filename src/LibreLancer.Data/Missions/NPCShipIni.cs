// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System.Collections.Generic;
using LibreLancer.Ini;
using System;
using LibreLancer.Data.IO;

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
        public int Level;
        [Entry("ship_archetype")] public string ShipArchetype;
        [Entry("pilot")] public string Pilot;
        [Entry("state_graph")] public string StateGraph;
        [Entry("npc_class")] public List<string> NpcClass = new List<string>();

        [EntryHandler("level", MinComponents = 1)]
        private void LevelEntry(Entry e)
        {
            var level = e[0].ToString();
            var index = level?.IndexOfAny("0123456789".ToCharArray());

            if (index is null)
            {
                Level = 0;
                return;
            }

            _ = int.TryParse(level.AsSpan(index.Value), out Level);
        }
    }
}
