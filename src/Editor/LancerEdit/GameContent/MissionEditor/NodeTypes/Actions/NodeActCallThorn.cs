using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActCallThorn : BlueprintNode
{
    protected override string Name => "Call Thorn";

    private readonly Act_CallThorn data;
    public NodeActCallThorn(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_CallThorn(action);
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
    }
}
