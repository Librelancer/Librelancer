using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActSave : NodeTriggerEntry
{
    public override string Name => "Save Game";

    public readonly Act_Save Data;

    public ActSave(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_Save(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));

        Outputs.Add(new NodePin(this, LinkType.Trigger, PinKind.Output));
    }

    public override void OnLinkCreated(NodeLink link)
    {
        Data.Trigger = (link.EndPin.OwnerNode as NodeMissionTrigger)!.Data.Nickname;
    }

    public override void OnLinkRemoved(NodeLink link)
    {
        if (link.EndPin.OwnerNode == this)
        {
            Data.Trigger = string.Empty;
        }
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        var text = string.IsNullOrWhiteSpace(Data.Trigger) ? "No Trigger" : Data.Trigger;

        Controls.DisabledInputTextId("Trigger", text);

        Controls.IdsInputStringUndo("IDS", gameData, popup, undoBuffer, () => ref Data.Ids);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
