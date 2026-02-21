using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActGiveObjectList : NodeTriggerEntry
{
    public override string Name => "Give Object List";

    public readonly Act_GiveObjList Data;
    public ActGiveObjectList(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_GiveObjList(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.InputTextIdUndo("List", undoBuffer, () => ref Data.List);
        Controls.InputTextIdUndo("Target", undoBuffer, () => ref Data.Target);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }

    public override MissionCondition CloneCondition() => null;
    public override MissionAction CloneAction()
    {
        return new MissionAction(
            TriggerActions.Act_GiveObjList,
            BuildEntry()
        );
    }
}
