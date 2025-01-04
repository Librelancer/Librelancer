using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActSetOrientation : BlueprintNode
{
    protected override string Name => "Set Orientation";

    private readonly Act_SetOrient data;
    public NodeActSetOrientation(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_SetOrient(action);

        Inputs.Add(new NodePin(id++, this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Target", ref data.Target);
        Controls.InputFlQuaternion("Target", ref data.Orientation);
    }
}
