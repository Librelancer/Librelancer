using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActPObjectIdle : BlueprintNode
{
    protected override string Name => "PObject Idle";

    private readonly Act_PobjIdle data;
    public NodeActPObjectIdle(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_PobjIdle(action);

        Inputs.Add(new NodePin(id++, this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
    }
}
