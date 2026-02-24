using System.Linq;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActSetVibeLabelToShip : NodeTriggerEntry
{
    public override string Name => "Set Vibe Label to Ship";

    public readonly Act_SetVibeLblToShip Data;

    public ActSetVibeLabelToShip(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_SetVibeLblToShip(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        nodePopups.Combo("Vibe", undoBuffer, () => ref Data.Vibe);
        nodePopups.StringCombo("Label", undoBuffer, () => ref Data.Label, lookups.Labels);
        nodePopups.StringCombo("Ship", undoBuffer, () => ref Data.Ship, lookups.Ships);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }

    public override MissionCondition CloneCondition() => null;
    public override MissionAction CloneAction()
    {
        return new MissionAction(
            TriggerActions.Act_SetVibeLblToShip,
            BuildEntry()
        );
    }
}
