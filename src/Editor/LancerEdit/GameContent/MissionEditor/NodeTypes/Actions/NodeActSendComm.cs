using ImGuiNET;
using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActSendComm : BlueprintNode
{
    protected override string Name => "Send Comm";

    private readonly Act_SendComm data;
    public NodeActSendComm(ref int id, Act_SendComm data) : base(ref id, NodeColours.Action)
    {
        this.data = data;
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionScript missionScript)
    {
        Controls.InputTextId("Source", ref data.Source);
        Controls.InputTextId("Destination", ref data.Destination);
        Controls.InputTextId("Line", ref data.Line);
    }
}
