using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActAdjustHealth : BlueprintNode
{
    protected override string Name => "Adjust Health";

    private readonly Act_AdjHealth data;
    public NodeActAdjustHealth(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_AdjHealth(action);

        Inputs.Add(new NodePin(id++, this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Target", ref data.Target);
        ImGui.SliderFloat("Health", ref data.Adjustment, -1f, 1f, "%.2f", ImGuiSliderFlags.AlwaysClamp);
    }
}
