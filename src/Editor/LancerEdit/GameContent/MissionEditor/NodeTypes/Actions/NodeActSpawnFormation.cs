using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActSpawnFormation : BlueprintNode
{
    protected override string Name => "Spawn Formation";

    private readonly Act_SpawnFormation data;
    public NodeActSpawnFormation(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_SpawnFormation(action);
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Formation", ref data.Formation);
        // ImGui.InputFloat3("Position", ref data.Position); // TODO: Handle null position on formation
    }
}
