using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActDeactivateTrigger : BlueprintNode
{
    protected override string Name => "Deactivate Trigger";

    private readonly Act_DeactTrig data;
    public NodeActDeactivateTrigger(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_DeactTrig(action);

        Inputs.Add(new NodePin(id++, "Trigger", this, LinkType.Action, PinKind.Input));
        Outputs.Add(new NodePin(id++, "Trigger", this, LinkType.Trigger, PinKind.Output));

    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Trigger", ref data.Trigger);
    }
}
