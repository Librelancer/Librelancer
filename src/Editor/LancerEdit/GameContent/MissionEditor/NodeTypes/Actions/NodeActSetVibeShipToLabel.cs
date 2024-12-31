using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActSetVibeShipToLabel : BlueprintNode
{
    protected override string Name => "Set Vibe Ship to Label";

    private readonly Act_SetVibeShipToLbl data;

    public NodeActSetVibeShipToLabel(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_SetVibeShipToLbl(action);

        Inputs.Add(new NodePin(id++, "Trigger", this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        NodeActSetVibe.VibeComboBox(ref data.Vibe);
        Controls.InputTextId("Ship", ref data.Ship);
        Controls.InputTextId("Label", ref data.Label);
    }
}
