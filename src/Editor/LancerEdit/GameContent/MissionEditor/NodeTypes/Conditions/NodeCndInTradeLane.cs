using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class NodeCndInTradeLane : BlueprintNode
{
    protected override string Name => "In Trade Lane";

    private bool inTL;
    public NodeCndInTradeLane(ref int id, Entry entry) : base(ref id, NodeColours.Condition)
    {
        if (entry?.Count >= 1)
        {
            inTL = entry[0].ToString()!.Equals("yes", System.StringComparison.InvariantCultureIgnoreCase);
        }

        Inputs.Add(new NodePin(this, LinkType.Condition, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        MissionIni missionIni)
    {
        ImGui.Checkbox("In Trade Lane", ref inTL);
    }
}
