using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActEnableManeuver : BlueprintNode
{
    protected override string Name => "Enable Maneuver";

    public readonly Act_EnableManeuver Data;
    public NodeActEnableManeuver(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        Data = action is null ? new() : new Act_EnableManeuver(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        // ImGui.Combo("Maneuver");
        ImGui.Checkbox("Lock", ref Data.Lock);
    }
}
