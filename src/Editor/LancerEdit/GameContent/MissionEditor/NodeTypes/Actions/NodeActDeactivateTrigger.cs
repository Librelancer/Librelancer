using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActDeactivateTrigger : BlueprintNode
{
    protected override string Name => "Deactivate Trigger";

    private readonly Act_DeactTrig data;
    public NodeActDeactivateTrigger(ref int id, Act_DeactTrig data) : base(ref id, NodeColours.Action)
    {
        this.data = data;
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionScript missionScript)
    {
        Controls.InputTextId("Trigger", ref data.Trigger);
    }
}
