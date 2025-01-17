using System;
using System.Diagnostics;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActActivateTrigger : BlueprintNode
{
    protected override string Name => "Activate Trigger";

    public readonly Act_ActTrig Data;
    public NodeActActivateTrigger(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        Data = action is null ? new() : new Act_ActTrig(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
        Outputs.Add(new NodePin(this, LinkType.Trigger, PinKind.Output, linkCapacity: 1));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        MissionIni missionIni)
    {
        var text = string.IsNullOrWhiteSpace(Data.Trigger) ? "No Trigger" : Data.Trigger;

        ImGui.BeginDisabled();
        Controls.InputTextId("Trigger", ref text);
        ImGui.EndDisabled();
    }

    public override void OnLinkCreated(NodeLink link)
    {
        if (link.StartPin.OwnerNode == this)
        {
            Data.Trigger = (link.EndPin.OwnerNode as NodeMissionTrigger)!.Data.Nickname;
        }
    }

    public override void OnLinkRemoved(NodeLink link)
    {
        if (link.EndPin.OwnerNode == this)
        {
            Data.Trigger = string.Empty;
        }
    }
}
