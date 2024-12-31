using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActGiveObjectList : BlueprintNode
{
    protected override string Name => "Give Object List";

    private readonly Act_GiveObjList data;
    public NodeActGiveObjectList(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_GiveObjList(action);
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("List", ref data.List);
        Controls.InputTextId("Target", ref data.Target);
    }
}
