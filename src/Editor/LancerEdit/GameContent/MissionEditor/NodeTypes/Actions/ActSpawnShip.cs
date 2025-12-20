using System.Linq;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActSpawnShip : NodeTriggerEntry
{
    public override string Name => "Spawn Ship";

    public readonly Act_SpawnShip Data;
    public ActSpawnShip(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_SpawnShip(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        nodePopups.StringCombo("Ship", undoBuffer, () => ref Data.Ship, lookups.Ships);
        nodePopups.StringCombo("Objective List", undoBuffer, () => ref Data.ObjList, lookups.ObjLists);
        Controls.InputOptionalVector3Undo("Position", undoBuffer, () => ref Data.Position);
        Controls.InputOptionalQuaternionUndo("Orientation", undoBuffer, () => ref Data.Orientation);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
