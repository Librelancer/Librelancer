using ImGuiNET;
using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActRelocateShip : BlueprintNode
{
    protected override string Name => "Set Initial Player Position";

    private readonly Act_RelocateShip data;
    public NodeActRelocateShip(ref int id, Act_RelocateShip data) : base(ref id, NodeColours.Action)
    {
        this.data = data;
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionScript missionScript)
    {
        ImGui.InputFloat3("Position", ref data.Position);
        // TODO: Orientation can be null?
        // Controls.InputFlQuaternion("Orientation", ref data.Orientation);
    }
}
