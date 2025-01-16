using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class NodeCndLocationEnter : BlueprintNode
{
    protected override string Name => "On Location Enter";

    private string location = string.Empty;
    private string @base = string.Empty;
    public NodeCndLocationEnter(ref int id, Entry entry) : base(ref id, NodeColours.Condition)
    {
        if (entry?.Count >= 2)
        {
            location = entry[0].ToString();
            @base = entry[1].ToString();
        }

        Inputs.Add(new NodePin(this, LinkType.Condition, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Location", ref location);
        Controls.InputTextId("Base", ref @base);
    }
}
