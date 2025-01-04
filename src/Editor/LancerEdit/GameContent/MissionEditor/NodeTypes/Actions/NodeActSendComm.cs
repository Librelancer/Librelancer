using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActSendComm : BlueprintNode
{
    protected override string Name => "Send Comm";

    private readonly Act_SendComm data;
    public NodeActSendComm(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_SendComm(action);

        Inputs.Add(new NodePin(id++, this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Source", ref data.Source);
        Controls.InputTextId("Destination", ref data.Destination);
        Controls.InputTextId("Line", ref data.Line);
    }
}
