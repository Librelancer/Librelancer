// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using System.Collections.Generic;
using LibreLancer.Ini;
namespace LibreLancer.Data.Missions
{
    public class MissionIni : IniFile
    {
        [Section("Mission")]
        public MissionInfo Info;
        [Section("NPC")]
        public List<MissionNPC> NPCs = new List<MissionNPC>();
        [Section("Trigger")]
        public List<MissionTrigger> Triggers = new List<MissionTrigger>();
        [Section("NNObjective")]
        public List<NNObjective> Objectives = new List<NNObjective>();
        [Section("Dialog")]
        public List<MissionDialog> Dialogs = new List<MissionDialog>();
        [Section("MsnShip")]
        public List<MissionShip> Ships = new List<MissionShip>();
        [Section("MsnSolar")]
        public List<MissionSolar> Solars = new List<MissionSolar>();
        [Section("MsnFormation")]
        public List<MissionFormation> Formations = new List<MissionFormation>();
        [Section("MsnLoot")]
        public List<MissionLoot> Loots = new List<MissionLoot>();
        [Section("ObjList")]
        public List<ObjList> ObjLists = new List<ObjList>();

        public MissionIni(string path)
        {
            ParseAndFill(path);
        }
    }
    public class MissionInfo
    {
        [Entry("mission_title")]
        public int MissionTitle;
        [Entry("mission_offer")]
        public int MissionOffer;
        [Entry("reward")]
        public int Reward;
        [Entry("npc_ship_file")]
        public string NpcShipFile;
    }

}
