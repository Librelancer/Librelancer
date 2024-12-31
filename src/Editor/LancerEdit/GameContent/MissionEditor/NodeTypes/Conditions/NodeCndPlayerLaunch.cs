using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class NodeCndPlayerLaunch : BlueprintNode
{
    protected override string Name => "On Player Launch";

    public NodeCndPlayerLaunch(ref int id, Entry entry) : base(ref id, NodeColours.Condition)
    {
        Inputs.Add(new NodePin(id++, "Trigger", this, LinkType.Condition, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
    }
}
