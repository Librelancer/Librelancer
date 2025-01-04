using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActAddRtc : BlueprintNode
{
    protected override string Name => "Add Real-Time Cutscene";

    private readonly Act_AddRTC data;
    public NodeActAddRtc(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_AddRTC(action);

        Inputs.Add(new NodePin(id++, this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("RTC", ref data.RTC);
    }
}
