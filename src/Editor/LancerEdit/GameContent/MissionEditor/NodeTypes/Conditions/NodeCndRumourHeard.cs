using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

// TODO: Figure out what the inputs to rumours are. The ones here are pure speculation as we have no examples.
public class NodeCndRumourHeard : BlueprintNode
{
    protected override string Name => "Has Rumour Been Heard";

    private int rumourId;
    private bool hasHeardRumour;
    public NodeCndRumourHeard(ref int id, Entry entry) : base(ref id, NodeColours.Condition)
    {
        Inputs.Add(new NodePin(this, LinkType.Condition, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        ImGui.Checkbox("Has Heard Rumour", ref hasHeardRumour);
        ImGui.InputInt("Rumour Id", ref rumourId);
    }
}
