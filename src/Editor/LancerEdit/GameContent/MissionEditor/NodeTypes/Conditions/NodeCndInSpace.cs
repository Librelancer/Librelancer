using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class NodeCndInSpace : BlueprintNode
{
    protected override string Name => "In Space";

    private bool inSpace;
    public NodeCndInSpace(ref int id, Entry entry) : base(ref id, NodeColours.Condition)
    {
        if (entry.Count >= 1)
        {
            inSpace = entry[0].ToString()!.Equals("yes", System.StringComparison.InvariantCultureIgnoreCase);
        }

        Inputs.Add(new NodePin(id++, this, LinkType.Condition, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        ImGui.Checkbox("In Space", ref inSpace);
    }
}
