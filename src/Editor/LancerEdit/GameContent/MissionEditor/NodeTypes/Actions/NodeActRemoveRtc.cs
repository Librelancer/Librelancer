using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActRemoveRtc : BlueprintNode
{
    protected override string Name => "Remove Real-Time Cutscene";

    private readonly Act_RemoveRTC data;
    public NodeActRemoveRtc(ref int id, Act_RemoveRTC data) : base(ref id, NodeColours.Action)
    {
        this.data = data;
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionScript missionScript)
    {
        Controls.InputTextId("RTC", ref data.RTC);
    }
}
