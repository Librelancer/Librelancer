using System.Linq;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActSetVibeShipToLabel : NodeTriggerEntry
{
    public override string Name => "Set Vibe Ship to Label";

    public readonly Act_SetVibeShipToLbl Data;

    public ActSetVibeShipToLabel(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_SetVibeShipToLbl(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {

        nodePopups.Combo("Vibe", undoBuffer, () => ref Data.Vibe);
        nodePopups.StringCombo("Ship", undoBuffer, () => ref Data.Ship, lookups.Ships);
        nodePopups.StringCombo("Label", undoBuffer, () => ref Data.Label, lookups.Labels);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }

    public override MissionCondition CloneCondition() => null;
    public override MissionAction CloneAction()
    {
        return new MissionAction(
            TriggerActions.Act_SetVibeShipToLbl,
            BuildEntry()
        );
    }
}
