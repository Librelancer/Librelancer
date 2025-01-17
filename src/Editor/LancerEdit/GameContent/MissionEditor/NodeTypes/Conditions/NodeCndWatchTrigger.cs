using System;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class NodeCndWatchTrigger : BlueprintNode
{
    protected override string Name => "On Trigger State Change";

    public bool TriggerOn;
    public string Trigger;
    public NodeCndWatchTrigger(ref int id, Entry entry) : base(ref id, NodeColours.Condition)
    {
        if (entry?.Count >= 2)
        {
            Trigger = entry[0].ToString();
            TriggerOn = entry[1].ToString()!.Equals("on", StringComparison.InvariantCultureIgnoreCase);
        }

        Inputs.Add(new NodePin(this, LinkType.Condition, PinKind.Input));
        Outputs.Add(new NodePin(this, LinkType.Trigger, PinKind.Output));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        MissionIni missionIni)
    {
        ImGui.Checkbox("Trigger On", ref TriggerOn);
        Controls.HelpMarker(
            "If true then the conditional will only be successful if the linked trigger is set to active.",
            true);
    }

    public override void OnLinkCreated(NodeLink link)
    {
        if (link.StartPin.OwnerNode == this)
        {
            Trigger = (link.EndPin.OwnerNode as NodeMissionTrigger)!.Data.Nickname;
        }
    }

    public override void OnLinkRemoved(NodeLink link)
    {
        if (link.EndPin.OwnerNode == this)
        {
            Trigger = string.Empty;
        }
    }
}
