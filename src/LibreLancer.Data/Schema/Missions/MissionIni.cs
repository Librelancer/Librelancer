// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System.Collections.Generic;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Missions;

[ParsedIni]
[IgnoreSection("Nodes")]
public partial class MissionIni
{
    [Section("Mission")]
    public MissionInfo? Info;
    [Section("NPC")]
    public List<MissionNPC> NPCs = [];
    [Section("Trigger")]
    public List<MissionTrigger> Triggers = [];
    [Section("NNObjective")]
    public List<NNObjective> Objectives = [];
    [Section("Dialog")]
    public List<MissionDialog> Dialogs = [];
    [Section("MsnShip")]
    public List<MissionShip> Ships = [];
    [Section("MsnSolar")]
    public List<MissionSolar> Solars = [];
    [Section("MsnFormation")]
    public List<MissionFormation> Formations = [];
    [Section("MsnLoot")]
    public List<MissionLoot> Loots = [];
    [Section("ObjList")]
    public List<ObjList> ObjLists = [];

    public NPCShipIni? ShipIni;
    public MissionIni(string path, FileSystem vfs)
    {
        ParseIni(path, vfs);
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
    public string? NpcShipFile;
}
