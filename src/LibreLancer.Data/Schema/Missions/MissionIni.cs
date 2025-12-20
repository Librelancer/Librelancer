// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Missions
{
    [ParsedIni]
    [IgnoreSection("Nodes")]
    public partial class MissionIni
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

        public NPCShipIni ShipIni;
        public MissionIni(string path, FileSystem vfs)
        {
            ParseIni(path, vfs);

            foreach (var objective in Objectives)
            {
                objective.TypeData = new NNObjectiveType
                {
                    Type = objective.Type[0]
                };

                if (objective.Type[0] == "ids")
                {
                    objective.TypeData.NameIds = int.Parse(objective.Type[1]);
                }
                else if (objective.Type[0] == "rep_inst" || objective.Type[0] == "navmarker")
                {
                    objective.TypeData.System = objective.Type[1];
                    objective.TypeData.NameIds = int.Parse(objective.Type[2]);
                    objective.TypeData.ExplanationIds = int.Parse(objective.Type[3]);
                    objective.TypeData.Position = new Vector3(
                        float.Parse(objective.Type[4], NumberFormatInfo.InvariantInfo),
                        float.Parse(objective.Type[5], NumberFormatInfo.InvariantInfo),
                        float.Parse(objective.Type[6], NumberFormatInfo.InvariantInfo));

                    if (objective.Type[0] == "rep_inst")
                    {
                        objective.TypeData.SolarNickname = objective.Type[7];
                    }
                }
                else
                {
                    throw new InvalidOperationException("Invalid NNObjective Type Provided: " + objective.Type[0]);
                }
            }
        }
    }
    [ParsedSection]
    public partial class MissionInfo
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
