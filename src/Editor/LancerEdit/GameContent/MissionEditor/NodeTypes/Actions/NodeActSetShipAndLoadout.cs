using System.Linq;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActSetShipAndLoadout : BlueprintNode
{
    protected override string Name => "Set Ship and Loadout";

    public readonly Act_SetShipAndLoadout Data;
    public NodeActSetShipAndLoadout(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        Data = action is null ? new() : new Act_SetShipAndLoadout(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        MissionIni missionIni)
    {
        var ships = missionIni.Ships.Select(x => x.Nickname).ToArray();
        var loadouts = gameData.GameData.Loadouts.Select(x => x.Nickname).OrderBy(x => x).ToArray();

        nodePopups.StringCombo("Ship", Data.Ship, s => Data.Ship = s, ships);
        nodePopups.StringCombo("Loadout", Data.Loadout, s => Data.Loadout = s, loadouts);
    }
}
