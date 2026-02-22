using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActAddAmbient : NodeTriggerEntry
{
    public override string Name => "Add Ambient";

    public readonly Act_AddAmbient Data;
    public ActAddAmbient(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_AddAmbient(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.InputTextIdUndo("Script", undoBuffer, () => ref Data.Script);
        Controls.InputTextIdUndo("Base", undoBuffer, () => ref Data.Base);
        Controls.InputTextIdUndo("Room", undoBuffer, () => ref Data.Room);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }

    public override MissionCondition CloneCondition() => null;
    public override MissionAction CloneAction()
    {
        return new MissionAction(
            TriggerActions.Act_AddAmbient,
        BuildEntry()
        );
    }
}
