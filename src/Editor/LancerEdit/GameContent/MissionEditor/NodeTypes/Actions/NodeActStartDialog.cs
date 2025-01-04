using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActStartDialog : BlueprintNode
{
    protected override string Name => "Start Dialog";

    private readonly Act_StartDialog data;
    public NodeActStartDialog(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_StartDialog(action);

        Inputs.Add(new NodePin(id++, this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Dialog", ref data.Dialog);
    }
}
