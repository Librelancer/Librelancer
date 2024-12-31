using ImGuiNET;
using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActMovePlayer : BlueprintNode
{
    protected override string Name => "Move Player";

    private readonly Act_MovePlayer data;
    public NodeActMovePlayer(ref int id, Act_MovePlayer data) : base(ref id, NodeColours.Action)
    {
        this.data = data;
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionScript missionScript)
    {
        ImGui.InputFloat3("Position", ref data.Position);
        ImGui.InputFloat("Unknown", ref data.Unknown);
    }
}
