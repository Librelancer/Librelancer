using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActEnableManeuver : BlueprintNode
{
    protected override string Name => "Enable Maneuver";

    private readonly Act_EnableManeuver data;
    public NodeActEnableManeuver(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_EnableManeuver(action);

        Inputs.Add(new NodePin(id++, this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        // ImGui.Combo("Maneuver");
        ImGui.Checkbox("Lock", ref data.Lock);
    }
}
