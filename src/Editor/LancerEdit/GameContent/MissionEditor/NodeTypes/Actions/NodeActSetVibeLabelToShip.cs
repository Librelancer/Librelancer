using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActSetVibeLabelToShip : BlueprintNode
{
    protected override string Name => "Set Vibe Label to Ship";

    private readonly Act_SetVibeLblToShip data;

    public NodeActSetVibeLabelToShip(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_SetVibeLblToShip(action);
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        NodeActSetVibe.VibeComboBox(ref data.Vibe);
        Controls.InputTextId("Label", ref data.Label);
        Controls.InputTextId("Ship", ref data.Ship);
    }
}
