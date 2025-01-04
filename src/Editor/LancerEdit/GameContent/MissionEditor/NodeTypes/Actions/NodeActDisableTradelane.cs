using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActDisableTradelane : BlueprintNode
{
    protected override string Name => "Disable Tradelane";

    private readonly Act_DisableTradelane data;
    public NodeActDisableTradelane(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_DisableTradelane(action);

        Inputs.Add(new NodePin(id++, this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Target TL", ref data.Tradelane);
    }
}
