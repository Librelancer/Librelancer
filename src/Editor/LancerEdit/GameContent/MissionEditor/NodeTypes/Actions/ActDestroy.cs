using System.Linq;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActDestroy : NodeTriggerEntry
{
    public override string Name => "Destroy";

    public readonly Act_Destroy Data;
    public ActDestroy(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_Destroy(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        nodePopups.StringCombo("Target", undoBuffer, () => ref Data.Target, lookups.ShipsSolarsAndLabels);
        nodePopups.Combo("Kind", undoBuffer, () => ref Data.Kind);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }

    public override MissionCondition CloneCondition() => null;
    public override MissionAction CloneAction()
    {
        return new MissionAction(
            TriggerActions.Act_Destroy,
            BuildEntry()
        );
    }
}
