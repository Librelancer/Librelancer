using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActDockRequest : BlueprintNode
{
    protected override string Name => "Start Dock Request with Object";

    private readonly Act_DockRequest data;
    public NodeActDockRequest(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_DockRequest(action);

        Inputs.Add(new NodePin(id++, "Trigger", this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Object", ref data.Object);
    }
}
