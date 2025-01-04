using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActEnableEncounter : BlueprintNode
{
    protected override string Name => "Enable Encounter";

    private readonly Act_EnableEnc data;
    public NodeActEnableEncounter(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_EnableEnc(action);

        Inputs.Add(new NodePin(id++, "Trigger", this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Encounter", ref data.Encounter);
    }
}
