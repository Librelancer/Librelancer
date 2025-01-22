using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;
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

    public override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.InputTextId("Sound Id", ref Data.ID);
        Controls.IdsInputString("Title IDS", gameData, popup, ref Data.Title, (ids) => Data.Title = ids);
        Controls.IdsInputString("Contents IDS", gameData, popup, ref Data.Contents, (ids) => Data.Title = ids);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
