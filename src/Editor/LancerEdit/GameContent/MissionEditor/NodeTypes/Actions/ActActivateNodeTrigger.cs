using System;
using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActActivateNodeTrigger : NodeTriggerEntry
{
    public override string Name => "Activate Trigger";

    public readonly Act_ActTrig Data;
    public ActActivateNodeTrigger(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_ActTrig(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
        Outputs.Add(new NodePin(this, LinkType.Trigger, PinKind.Output, linkCapacity: 1));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        var text = string.IsNullOrWhiteSpace(Data.Trigger) ? "No Trigger" : Data.Trigger;

        Controls.DisabledInputTextId("Trigger", text);
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
        if (link.StartPin.OwnerNode == this)
        {
            Data.Trigger = string.Empty;
        }
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
