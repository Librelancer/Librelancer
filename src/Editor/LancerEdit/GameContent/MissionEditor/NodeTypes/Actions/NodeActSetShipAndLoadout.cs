using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActSetShipAndLoadout : BlueprintNode
{
    protected override string Name => "Set Ship and Loadout";

    private readonly Act_SetShipAndLoadout data;
    public NodeActSetShipAndLoadout(ref int id, Act_SetShipAndLoadout data) : base(ref id, NodeColours.Action)
    {
        this.data = data;
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionScript missionScript)
    {
        Controls.InputTextId("Ship", ref data.Ship);
        Controls.InputTextId("Loadout", ref data.Loadout);
    }
}
