using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActSetOffer : BlueprintNode
{
    protected override string Name => "Set Mission Offer";

    private readonly Act_SetOffer data;
    public NodeActSetOffer(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_SetOffer(action);

        Inputs.Add(new NodePin(id++, "Trigger", this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.IdsInputString("IDS", gameData, popup, ref data.Ids, (ids) => data.Ids = ids);
    }
}
