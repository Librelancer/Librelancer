using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActCallThorn : NodeTriggerEntry
{
    public override string Name => "Call Thorn";

    public readonly Act_CallThorn Data;
    public ActCallThorn(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_CallThorn(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.InputTextUndo("Script", undoBuffer, () => ref Data.Thorn);
        Controls.InputTextIdUndo("Main Object", undoBuffer, () => ref Data.MainObject);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
