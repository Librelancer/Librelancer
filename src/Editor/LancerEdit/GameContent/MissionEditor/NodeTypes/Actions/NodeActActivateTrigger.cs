using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActActivateTrigger : BlueprintNode
{
    protected override string Name => "Activate Trigger";

    private readonly Act_ActTrig data;
    public NodeActActivateTrigger(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_ActTrig(action);
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Trigger", ref data.Trigger);
    }
}
