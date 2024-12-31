using System;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class NodeCndWatchTrigger : BlueprintNode
{
    protected override string Name => "On Trigger State Change";

    private bool triggerOn;
    public NodeCndWatchTrigger(ref int id, Entry entry) : base(ref id, NodeColours.Condition)
    {
        if (entry.Count >= 2)
        {
            // TODO: Link trigger
            triggerOn = entry[1].ToString()!.Equals("on", StringComparison.InvariantCultureIgnoreCase);
        }

        Inputs.Add(new NodePin(id++, "Trigger", this, LinkType.Condition, PinKind.Input));
        Outputs.Add(new NodePin(id++, "Trigger", this, LinkType.Trigger, PinKind.Output));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        ImGui.Checkbox("Trigger On", ref triggerOn);
        Controls.HelpMarker(
            "If true then the conditional will only be successful if the linked trigger is set to active.",
            true);
    }
}
