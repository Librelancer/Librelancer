using ImGuiNET;
using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActPopupDialog : BlueprintNode
{
    protected override string Name => "Popup Dialog";

    private readonly Act_PopupDialog data;
    public NodeActPopupDialog(ref int id, Act_PopupDialog data) : base(ref id, NodeColours.Action)
    {
        this.data = data;
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionScript missionScript)
    {
        Controls.InputTextId("Sound Id", ref data.ID);
        Controls.IdsInputString("Title IDS", gameData, popup, ref data.Title, (ids) => data.Title = ids);
        Controls.IdsInputInfocard("Contents IDS", gameData, popup, ref data.Contents, (ids) => data.Title = ids);
    }
}
