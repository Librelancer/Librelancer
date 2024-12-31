using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActAddRtc : BlueprintNode
{
    protected override string Name => "Add Real-Time Cutscene";

    private readonly Act_AddRTC data;
    public NodeActAddRtc(ref int id, Act_AddRTC data) : base(ref id, NodeColours.Action)
    {
        this.data = data;
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionScript missionScript)
    {
        Controls.InputTextId("RTC", ref data.RTC);
    }
}
