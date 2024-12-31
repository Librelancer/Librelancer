using ImGuiNET;
using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActSpawnFormation : BlueprintNode
{
    protected override string Name => "Spawn Formation";

    private readonly Act_SpawnFormation data;
    public NodeActSpawnFormation(ref int id, Act_SpawnFormation data) : base(ref id, NodeColours.Action)
    {
        this.data = data;
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionScript missionScript)
    {
        Controls.InputTextId("Formation", ref data.Formation);
        // ImGui.InputFloat3("Position", ref data.Position); // TODO: Handle null position on formation
    }
}
