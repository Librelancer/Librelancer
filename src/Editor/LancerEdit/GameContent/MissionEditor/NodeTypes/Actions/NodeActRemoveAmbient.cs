using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActRemoveAmbient : BlueprintNode
{
    protected override string Name => "Remove Ambient";

    private readonly Act_RemoveAmbient data;
    public NodeActRemoveAmbient(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_RemoveAmbient(action);
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Script", ref data.Script);
    }
}
