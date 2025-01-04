using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActSpawnShip : BlueprintNode
{
    protected override string Name => "Spawn Ship";
    protected override float NodeInnerWidth => 320;

    private readonly Act_SpawnShip data;
    public NodeActSpawnShip(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_SpawnShip(action);

        Inputs.Add(new NodePin(id++, this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Ship", ref data.Ship);
        Controls.InputTextId("Object List", ref data.ObjList);
        // TODO: Handle null values for pos and orient
        Controls.InputVec3Nullable("Position", ref data.Position);
        Controls.InputFlQuaternionNullable("Orientation", ref data.Orientation);
    }
}
