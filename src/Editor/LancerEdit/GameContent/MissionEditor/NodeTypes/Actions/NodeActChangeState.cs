using ImGuiNET;
using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActChangeState : BlueprintNode
{
    protected override string Name => "Change State";

    private readonly Act_ChangeState Data;
    public NodeActChangeState(ref int id, Act_ChangeState data) : base(ref id, NodeColours.Action)
    {
        Data = data;
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionScript missionScript)
    {
        ImGui.Checkbox("Success", ref Data.Succeed);
    }
}
