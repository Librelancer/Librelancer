using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActCallThorn : BlueprintNode
{
    protected override string Name => "Call Thorn";

    private readonly Act_CallThorn data;
    public NodeActCallThorn(ref int id, Act_CallThorn data) : base(ref id, NodeColours.Action)
    {
        this.data = data;
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionScript missionScript)
    {
    }
}
