using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActRemoveAmbient : BlueprintNode
{
    protected override string Name => "Remove Ambient";

    private readonly Act_RemoveAmbient data;
    public NodeActRemoveAmbient(ref int id, Act_RemoveAmbient data) : base(ref id, NodeColours.Action)
    {
        this.data = data;
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionScript missionScript)
    {
        Controls.InputTextId("Script", ref data.Script);
    }
}
