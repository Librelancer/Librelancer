using ImGuiNET;
using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActAdjustAccount : BlueprintNode
{
    protected override string Name => "Adjust Account";

    private readonly Act_AdjAcct data;
    public NodeActAdjustAccount(ref int id, Act_AdjAcct data) : base(ref id, NodeColours.Action)
    {
        this.data = data;
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionScript missionScript)
    {
        ImGui.InputInt("Amount", ref data.Amount);
    }
}
