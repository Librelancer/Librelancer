using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActSpawnSolar : BlueprintNode
{
    protected override string Name => "Spawn Solar";

    private readonly Act_SpawnSolar data;
    public NodeActSpawnSolar(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_SpawnSolar(action);

        Inputs.Add(new NodePin(id++, "Trigger", this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Solar", ref data.Solar);
    }
}
