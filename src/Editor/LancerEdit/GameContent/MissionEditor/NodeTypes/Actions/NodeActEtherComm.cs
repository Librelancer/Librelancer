using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActEtherComm : BlueprintNode
{
    protected override string Name => "Ether Comm";

    private readonly Act_EtherComm data;
    public NodeActEtherComm(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_EtherComm(action);

        Inputs.Add(new NodePin(id++, "Trigger", this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Line", ref data.Line);
        Controls.InputTextId("Voices", ref data.Voice);
    }
}
