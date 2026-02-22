using System.Linq;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActSetVibeLabel : NodeTriggerEntry
{
    public override string Name => "Set Vibe Label";

    public readonly Act_SetVibeLbl Data;

    public ActSetVibeLabel(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_SetVibeLbl(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        nodePopups.Combo("Vibe", undoBuffer, () => ref Data.Vibe);
        nodePopups.StringCombo("Label 1", undoBuffer, () => ref Data.Label1, lookups.Labels);
        nodePopups.StringCombo("Label 2", undoBuffer, () => ref Data.Label2, lookups.Labels);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }

    public override MissionCondition CloneCondition() => null;
    public override MissionAction CloneAction()
    {
        return new MissionAction(
            TriggerActions.Act_SetVibeLbl,
            BuildEntry()
        );
    }
}
