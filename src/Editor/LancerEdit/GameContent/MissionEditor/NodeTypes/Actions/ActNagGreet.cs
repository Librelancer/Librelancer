using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActNagGreet : NodeTriggerEntry
{
    public override string Name => "Nag Greet";

    public readonly Act_NagGreet Data;
    public ActNagGreet(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_NagGreet(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.InputTextIdUndo("Source", undoBuffer, () => ref Data.Source);
        Controls.InputTextIdUndo("Target", undoBuffer, () => ref Data.Target);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }

    public override MissionCondition CloneCondition() => null;
    public override MissionAction CloneAction()
    {
        return new MissionAction(
            TriggerActions.Act_NagGreet,
            BuildEntry()
        );
    }
}
