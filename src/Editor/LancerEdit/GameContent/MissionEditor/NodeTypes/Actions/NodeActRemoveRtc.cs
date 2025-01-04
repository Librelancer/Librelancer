using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActRemoveRtc : BlueprintNode
{
    protected override string Name => "Remove Real-Time Cutscene";

    private readonly Act_RemoveRTC data;
    public NodeActRemoveRtc(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_RemoveRTC(action);

        Inputs.Add(new NodePin(id++, this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("RTC", ref data.RTC);
    }
}
