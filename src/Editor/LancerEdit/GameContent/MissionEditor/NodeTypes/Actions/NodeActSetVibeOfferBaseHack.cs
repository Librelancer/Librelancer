using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActSetVibeOfferBaseHack : BlueprintNode
{
    protected override string Name => "Set Base To Friendly";

    private readonly Act_SetVibeOfferBaseHack data;
    public NodeActSetVibeOfferBaseHack(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_SetVibeOfferBaseHack(action);

        Inputs.Add(new NodePin(id++, this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Base", ref data.Id);
    }
}
