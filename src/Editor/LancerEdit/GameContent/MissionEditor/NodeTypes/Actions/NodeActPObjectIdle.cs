using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActPObjectIdle : BlueprintNode
{
    protected override string Name => "PObject Idle";

    private readonly Act_PobjIdle data;
    public NodeActPObjectIdle(ref int id, Act_PobjIdle data) : base(ref id, NodeColours.Action)
    {
        this.data = data;
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionScript missionScript)
    {
    }
}
