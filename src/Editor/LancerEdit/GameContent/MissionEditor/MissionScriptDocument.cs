using System;
using System.Collections.Generic;
using LancerEdit.GameContent.MissionEditor.NodeTypes;
using LibreLancer.ContentEdit;
using LibreLancer.Data;
using LibreLancer.Data.GameData;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor;


public class MissionScriptDocument
{
    public MissionInfo Info;
    public SortedDictionary<string, ShipArch> NpcShips;
    public SortedDictionary<string, ScriptNPC> Npcs;
    public SortedDictionary<string, DocumentObjective> Objectives;
    public SortedDictionary<string, ScriptDialog> Dialogs;
    public SortedDictionary<string, ScriptShip> Ships;
    public SortedDictionary<string, ScriptSolar> Solars;
    public SortedDictionary<string, ScriptFormation> Formations;
    public SortedDictionary<string, ScriptLoot> Loots;
    public SortedDictionary<string, ScriptAiCommands> ObjLists;

    static SortedDictionary<string, TValue> ToSortedDictionary<TValue>(Dictionary<string, TValue> dict)
    {
        var lst = new SortedDictionary<string, TValue>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in dict)
            lst.Add(pair.Key, pair.Value);
        return lst;
    }

    public static MissionScriptDocument FromFile(string file, GameDataContext gameData, out List<MissionTrigger> iniTriggers)
    {
        var iniFile = new MissionIni(file, null);
        var srcNpcIni = iniFile?.Info?.NpcShipFile ?? "";
        if (!string.IsNullOrWhiteSpace(srcNpcIni))
        {
            var npcPath = gameData.GameData.VFS.GetBackingFileName(gameData.GameData.Items.DataPath(srcNpcIni));
            if (npcPath is not null)
            {
                iniFile.ShipIni = new NPCShipIni(npcPath, null);
            }
            else
            {
                srcNpcIni = "";
            }
        }
        var gameScript = new MissionScript(iniFile, gameData.GameData.Items);
        var missionIni = new MissionScriptDocument(gameScript);
        missionIni.Info ??= new MissionInfo();
        missionIni.Info.NpcShipFile = srcNpcIni;
        iniTriggers = iniFile.Triggers;
        return missionIni;
    }

    public MissionScriptDocument(MissionScript script)
    {
        Info = script.Info;
        NpcShips = ToSortedDictionary(script.NpcShips);
        Npcs = ToSortedDictionary(script.NPCs);

        Dialogs = ToSortedDictionary(script.Dialogs);
        Ships = ToSortedDictionary(script.Ships);
        Solars = ToSortedDictionary(script.Solars);
        Formations = ToSortedDictionary(script.Formations);
        Loots = ToSortedDictionary(script.Loot);
        ObjLists = ToSortedDictionary(script.ObjLists);
        Objectives = new (StringComparer.OrdinalIgnoreCase);
        foreach (var obj in script.Objectives)
            Objectives[obj.Key] = new() { Nickname = obj.Key, Data = obj.Value };
    }

    public void Save(
        string filename,
        GameDataContext gameData,
        IEnumerable<NodeMissionTrigger> triggers,
        IEnumerable<SavedNode> nodes)
    {
        if (!string.IsNullOrWhiteSpace(Info.NpcShipFile))
        {
            var npcPath = gameData.GameData.VFS.GetBackingFileName(gameData.GameData.Items.DataPath(Info.NpcShipFile));
            if (npcPath is not null)
            {
                var npcBuilder = new IniBuilder();
                foreach(var npc in NpcShips)
                    IniSerializer.SerializeShipArch(npc.Value, npcBuilder);
                IniWriter.WriteIniFile(npcPath, npcBuilder.Sections);
            }
        }

        IniBuilder ini = new();

        ini.Section("Mission")
            .Entry("mission_title", Info.MissionTitle)
            .Entry("mission_offer", Info.MissionOffer)
            .OptionalEntry("reward", Info.Reward)
            .Entry("npc_ship_file", Info.NpcShipFile);

        foreach (var npc in Npcs.Values)
        {
            IniSerializer.SerializeScriptNpc(npc, ini);
        }

        foreach (var objective in Objectives.Values)
        {
            objective.Data.Nickname = objective.Nickname;
            IniSerializer.SerializeMissionObjective(objective.Data, ini);
        }

        foreach (var loot in Loots.Values)
        {
            IniSerializer.SerializeScriptLoot(loot, ini);
        }

        foreach (var dialog in Dialogs.Values)
        {
            IniSerializer.SerializeScriptDialog(dialog, ini);
        }

        foreach (var objectiveList in ObjLists.Values)
        {
            IniSerializer.SerializeScriptObjectiveList(objectiveList, ini);
        }

        foreach (var solar in Solars.Values)
        {
            IniSerializer.SerializeScriptSolar(solar, ini);
        }

        foreach (var ship in Ships.Values)
        {
            IniSerializer.SerializeScriptShip(ship, ini);
        }

        foreach (var formation in Formations.Values)
        {
            IniSerializer.SerializeScriptFormation(formation, ini);
        }

        foreach (var tr in triggers)
        {
            tr.WriteNode(ini);
        }

        var nodeSection = ini.Section("Nodes");
        foreach (var n in nodes)
        {
            n.Write(nodeSection);
        }

        IniWriter.WriteIniFile(filename, ini.Sections);
    }
}

public class DocumentObjective : NicknameItem
{
    public NNObjective Data = new();
}
