using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActSendComm : BlueprintNode
{
    protected override string Name => "Send Comm";

    public readonly Act_SendComm Data;
    public NodeActSendComm(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        Data = action is null ? new() : new Act_SendComm(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        MissionIni missionIni)
    {
        Controls.InputTextId("Source", ref Data.Source);
        Controls.InputTextId("Destination", ref Data.Destination);
        Controls.InputTextId("Line", ref Data.Line);
    }
}
