using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActSetOffer : BlueprintNode
{
    protected override string Name => "Set Mission Offer";

    public readonly Act_SetOffer Data;
    public NodeActSetOffer(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        Data = action is null ? new() : new Act_SetOffer(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.IdsInputString("IDS", gameData, popup, ref Data.Ids, (ids) => Data.Ids = ids);
    }
}
