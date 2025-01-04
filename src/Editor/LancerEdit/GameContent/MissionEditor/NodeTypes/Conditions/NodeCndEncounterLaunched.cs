using System;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class NodeCndEncounterLaunched : BlueprintNode
{
    protected override string Name => "On Encounter Launched";

    private string encounter = string.Empty;

    public NodeCndEncounterLaunched(ref int id, Entry entry) : base(ref id, NodeColours.Condition)
    {
        if (entry.Count >= 1)
        {
            encounter = entry[0].ToString();
        }

        Inputs.Add(new NodePin(id++, this, LinkType.Condition, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Encounter", ref encounter);
    }
}
