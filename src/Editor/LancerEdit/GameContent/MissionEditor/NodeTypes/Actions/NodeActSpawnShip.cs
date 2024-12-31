using ImGuiNET;
using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActSpawnShip : BlueprintNode
{
    protected override string Name => "Spawn Ship";

    private readonly Act_SpawnShip data;
    public NodeActSpawnShip(ref int id, Act_SpawnShip data) : base(ref id, NodeColours.Action)
    {
        this.data = data;
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionScript missionScript)
    {
        Controls.InputTextId("Ship", ref data.Ship);
        Controls.InputTextId("Object List", ref data.ObjList);
        // TODO: Handle null values for pos and orient
        // ImGui.InputFloat3("Position", ref data.Position);
        // Controls.InputTextId("Orientation", ref data.Orientation);
    }
}
