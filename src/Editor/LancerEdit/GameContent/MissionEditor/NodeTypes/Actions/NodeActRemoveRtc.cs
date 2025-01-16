using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActRemoveRtc : BlueprintNode
{
    protected override string Name => "Remove Real-Time Cutscene";

    public readonly Act_RemoveRTC Data;
    public NodeActRemoveRtc(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        Data = action is null ? new() :  new Act_RemoveRTC(action);;

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("RTC", ref Data.RTC);
    }
}
