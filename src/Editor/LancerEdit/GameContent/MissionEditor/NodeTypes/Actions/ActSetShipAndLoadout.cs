using System.Linq;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActSetShipAndLoadout : NodeTriggerEntry
{
    public override string Name => "Set Ship and Loadout";

    public readonly Act_SetShipAndLoadout Data;
    public ActSetShipAndLoadout(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_SetShipAndLoadout(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        nodePopups.StringCombo("Ship", undoBuffer, () => ref Data.Ship, lookups.Ships);
        nodePopups.StringCombo("Loadout", undoBuffer, () => ref Data.Loadout, gameData.LoadoutsByName);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }

    public override MissionCondition CloneCondition() => null;
    public override MissionAction CloneAction()
    {
        return new MissionAction(
            TriggerActions.Act_SetShipAndLoadout,
            BuildEntry()
        );
    }
}
