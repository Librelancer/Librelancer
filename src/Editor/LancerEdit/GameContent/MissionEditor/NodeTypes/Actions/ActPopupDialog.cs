using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActPopupDialog : NodeTriggerEntry
{
    public override string Name => "Popup Dialog";

    public readonly Act_PopupDialog Data;
    public ActPopupDialog(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_PopupDialog(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.InputTextIdUndo("Sound Id", undoBuffer, () => ref Data.ID);
        Controls.IdsInputStringUndo("Title IDS", gameData, popup, undoBuffer, () => ref Data.Title);
        Controls.IdsInputStringUndo("Contents IDS", gameData, popup, undoBuffer, () => ref Data.Contents);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
