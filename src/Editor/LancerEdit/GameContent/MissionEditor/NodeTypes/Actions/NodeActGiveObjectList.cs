using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActGiveObjectList : BlueprintNode
{
    protected override string Name => "Give Object List";

    private readonly Act_GiveObjList data;
    public NodeActGiveObjectList(ref int id, Act_GiveObjList data) : base(ref id, NodeColours.Action)
    {
        this.data = data;
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionScript missionScript)
    {
        Controls.InputTextId("List", ref data.List);
        Controls.InputTextId("Target", ref data.Target);
    }
}
