using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActSetShipAndLoadout : BlueprintNode
{
    protected override string Name => "Set Ship and Loadout";

    private readonly Act_SetShipAndLoadout data;
    public NodeActSetShipAndLoadout(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_SetShipAndLoadout(action);

        Inputs.Add(new NodePin(id++, this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Ship", ref data.Ship);
        Controls.InputTextId("Loadout", ref data.Loadout);
    }
}
