using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActLockDock : NodeTriggerEntry
{
    public override string Name => "Toggle Docking Lock";

    public readonly Act_LockDock Data;
    public ActLockDock(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_LockDock(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.InputTextIdUndo("Target", undoBuffer, () => ref Data.Target);
        Controls.InputTextIdUndo("Object", undoBuffer, () => ref Data.Object);
        Controls.CheckboxUndo("Lock", undoBuffer, () => ref Data.Lock);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
