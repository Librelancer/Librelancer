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
        Controls.InputTextId("Ship", ref Data.Ship);
        Controls.InputTextId("Loadout", ref Data.Loadout);
    }
}
