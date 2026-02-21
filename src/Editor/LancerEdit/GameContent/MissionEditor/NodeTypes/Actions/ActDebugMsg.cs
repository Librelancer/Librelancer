using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActDebugMsg : NodeTriggerEntry
{
    public override string Name => "Debug Message";

    public readonly Act_DebugMsg Data;
    public ActDebugMsg(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_DebugMsg(action);
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.InputTextUndo("Message", undoBuffer, () => ref Data.Message);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }

    public override MissionCondition CloneCondition() => null;
    public override MissionAction CloneAction()
    {
        return new MissionAction(
            TriggerActions.Act_DebugMsg,
            BuildEntry()
        );
    }
}
