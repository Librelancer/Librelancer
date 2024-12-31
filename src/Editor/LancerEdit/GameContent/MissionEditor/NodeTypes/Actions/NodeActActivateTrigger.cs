using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActActivateTrigger : BlueprintNode
{
    protected override string Name => "Activate Trigger";

    private readonly Act_ActTrig data;
    public NodeActActivateTrigger(ref int id, Act_ActTrig data) : base(ref id, NodeColours.Action)
    {
        this.data = data;
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionScript missionScript)
    {
        Controls.InputTextId("Trigger", ref data.Trigger);
    }
}
