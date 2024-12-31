using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActPopupDialog : BlueprintNode
{
    protected override string Name => "Popup Dialog";

    private readonly Act_PopupDialog data;
    public NodeActPopupDialog(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_PopupDialog(action);

        Inputs.Add(new NodePin(id++, "Trigger", this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Sound Id", ref data.ID);
        Controls.IdsInputString("Title IDS", gameData, popup, ref data.Title, (ids) => data.Title = ids);
        Controls.IdsInputInfocard("Contents IDS", gameData, popup, ref data.Contents, (ids) => data.Title = ids);
    }
}
