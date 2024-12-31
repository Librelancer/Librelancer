using ImGuiNET;
using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActDestroy : BlueprintNode
{
    protected override string Name => "Destroy";

    private readonly Act_Destroy data;
    public NodeActDestroy(ref int id, Act_Destroy data) : base(ref id, NodeColours.Action)
    {
        this.data = data;
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionScript missionScript)
    {
        Controls.InputTextId("Target", ref data.Target);
    }
}
