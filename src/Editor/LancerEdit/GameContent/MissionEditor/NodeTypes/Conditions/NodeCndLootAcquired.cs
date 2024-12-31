using System;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class NodeCndLootAcquired : BlueprintNode
{
    protected override string Name => "On Loot Acquired (Tractored)";

    private string target = string.Empty;
    private string sourceShip = string.Empty;

    public NodeCndLootAcquired(ref int id, Entry entry) : base(ref id, NodeColours.Condition)
    {
        if (entry.Count >= 2)
        {
            target = entry[0].ToString();
            sourceShip = entry[1].ToString();
        }

        Inputs.Add(new NodePin(id++, "Trigger", this, LinkType.Condition, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Source Ship", ref sourceShip);
        Controls.InputTextId("Target", ref target);
    }
}
