using System.Linq;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActSpawnShip : TriggerEntryNode
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
        ref NodeLookups lookups)
    {
        nodePopups.StringCombo("Ship", Data.Ship, s => Data.Ship = s, lookups.Ships);
        nodePopups.StringCombo("Objective List", Data.ObjList, s => Data.ObjList = s, lookups.ObjLists);
        Controls.InputVec3Nullable("Position", ref Data.Position);
        Controls.InputFlQuaternionNullable("Orientation", ref Data.Orientation);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
