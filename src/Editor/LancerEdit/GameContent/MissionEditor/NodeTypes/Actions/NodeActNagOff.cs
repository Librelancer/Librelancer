using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActNagOff : BlueprintNode
{
    protected override string Name => "Disable Nag";

    private readonly Act_NagOff data;
    public NodeActNagOff(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_NagOff(action);

        Inputs.Add(new NodePin(id++, this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Nag", ref data.Nag);
    }
}
