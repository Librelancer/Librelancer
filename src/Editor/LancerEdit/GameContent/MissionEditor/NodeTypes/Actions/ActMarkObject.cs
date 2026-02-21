using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActMarkObject : NodeTriggerEntry
{
    public override string Name => "Mark Object";

    public readonly Act_MarkObj Data;
    public ActMarkObject(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_MarkObj(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.InputTextIdUndo("Object", undoBuffer, () => ref Data.Object);
        Controls.CheckboxUndo("Important", undoBuffer, () => ref Data.Important);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }

    public override MissionCondition CloneCondition() => null;
    public override MissionAction CloneAction()
    {
        return new MissionAction(
            TriggerActions.Act_MarkObj,
            BuildEntry()
        );
    }
}
