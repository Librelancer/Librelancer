using System.Linq;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Missions;
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

    public override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        nodePopups.StringCombo("Ship", Data.Ship, s => Data.Ship = s, lookups.Ships);
        nodePopups.StringCombo("Loadout", Data.Loadout, s => Data.Loadout = s, gameData.LoadoutsByName);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
