using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActRelocateShip : NodeTriggerEntry
{
    public override string Name => "Set Initial Player Position";

    public readonly Act_RelocateShip Data;
    public ActRelocateShip(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_RelocateShip(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.InputFloat3Undo("Position", undoBuffer, () => ref Data.Position);
        // TODO: Orientation can be null?
        // Controls.InputFlQuaternion("Orientation", ref Data.Orientation);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }


    public override MissionCondition CloneCondition() => null;
    public override MissionAction CloneAction()
    {
        return new MissionAction(
            TriggerActions.Act_RelocateShip,
            BuildEntry()
        );
    }
}
