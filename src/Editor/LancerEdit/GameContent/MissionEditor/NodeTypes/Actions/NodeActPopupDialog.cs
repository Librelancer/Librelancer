using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActPopupDialog : TriggerEntryNode
{
    protected override string Name => "Popup Dialog";

    public readonly Act_PopupDialog Data;
    public NodeActPopupDialog(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        Data = action is null ? new() : new Act_PopupDialog(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        MissionIni missionIni)
    {
        Controls.InputTextId("Sound Id", ref Data.ID);
        Controls.IdsInputString("Title IDS", gameData, popup, ref Data.Title, (ids) => Data.Title = ids);
        Controls.IdsInputInfocard("Contents IDS", gameData, popup, ref Data.Contents, (ids) => Data.Title = ids);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
