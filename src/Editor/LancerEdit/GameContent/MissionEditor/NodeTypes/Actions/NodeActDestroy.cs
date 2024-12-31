using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActDestroy : BlueprintNode
{
    protected override string Name => "Destroy";

    private readonly Act_Destroy data;
    public NodeActDestroy(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_Destroy(action);
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Target", ref data.Target);
    }
}
