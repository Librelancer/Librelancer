using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActSpawnSolar : BlueprintNode
{
    protected override string Name => "Spawn Solar";

    private readonly Act_SpawnSolar data;
    public NodeActSpawnSolar(ref int id, Act_SpawnSolar data) : base(ref id, NodeColours.Action)
    {
        this.data = data;
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionScript missionScript)
    {
        Controls.InputTextId("Solar", ref data.Solar);
    }
}
