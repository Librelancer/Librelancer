using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActRevertCamera : BlueprintNode
{
    protected override string Name => "Revert Camera";

    private readonly Act_RevertCam data;
    public NodeActRevertCamera(ref int id, Act_RevertCam data) : base(ref id, NodeColours.Action)
    {
        this.data = data;
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionScript missionScript)
    {
    }
}
