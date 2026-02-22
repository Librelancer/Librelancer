using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActSetOrientation : NodeTriggerEntry
{
    public override string Name => "Set Orientation";

    public readonly Act_SetOrient Data;
    public ActSetOrientation(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_SetOrient(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.InputTextIdUndo("Target", undoBuffer, () => ref Data.Target);
        Controls.InputQuaternionUndo("Target", undoBuffer, () => ref Data.Orientation);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }

    public override MissionCondition CloneCondition() => null;
    public override MissionAction CloneAction()
    {
        return new MissionAction(
            TriggerActions.Act_SetOrient,
            BuildEntry()
        );
    }
}
