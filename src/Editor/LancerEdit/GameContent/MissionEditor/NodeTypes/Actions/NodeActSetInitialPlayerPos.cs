using ImGuiNET;
using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActSetInitialPlayerPos : BlueprintNode
{
    protected override string Name => "Set Initial Player Position";

    private readonly Act_SetInitialPlayerPos data;
    public NodeActSetInitialPlayerPos(ref int id, Act_SetInitialPlayerPos data) : base(ref id, NodeColours.Action)
    {
        this.data = data;
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionScript missionScript)
    {
        ImGui.InputFloat3("Position", ref data.Position);
        Controls.InputFlQuaternion("Orientation", ref data.Orientation);
    }
}
