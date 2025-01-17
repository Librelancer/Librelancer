using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActSpawnShip : BlueprintNode
{
    protected override string Name => "Spawn Ship";
    protected override float NodeInnerWidth => 320;

    public readonly Act_SpawnShip Data;
    public NodeActSpawnShip(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        Data = action is null ? new() : new Act_SpawnShip(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        MissionIni missionIni)
    {
        Controls.InputTextId("Ship", ref Data.Ship);
        Controls.InputTextId("Object List", ref Data.ObjList);
        // TODO: Handle null values for pos and orient
        Controls.InputVec3Nullable("Position", ref Data.Position);
        Controls.InputFlQuaternionNullable("Orientation", ref Data.Orientation);
    }
}
