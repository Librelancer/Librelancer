using ImGuiNET;
using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActMarkObject : BlueprintNode
{
    protected override string Name => "Mark Object";

    private readonly Act_MarkObj data;
    public NodeActMarkObject(ref int id, Act_MarkObj data) : base(ref id, NodeColours.Action)
    {
        this.data = data;
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionScript missionScript)
    {
        Controls.InputTextId("Object", ref data.Object);
        ImGui.InputInt("Value", ref data.Value); // TODO: An enum value of some kind
    }
}
