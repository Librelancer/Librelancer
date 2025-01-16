using System;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class NodeCndShipDistance : BlueprintNode
{
    protected override string Name => "On Ship Distance Change (Object)";

    private bool inside;
    private float distance;
    private string sourceShip;
    private string destObject;
    private bool tickAway;

    public NodeCndShipDistance(ref int id, Entry entry) : base(ref id, NodeColours.Condition)
    {
        if (entry?.Count >= 4)
        {
            inside = entry[0].ToString()!.Equals("inside", StringComparison.InvariantCultureIgnoreCase);
            sourceShip = entry[1].ToString();
            destObject = entry[2].ToString();
            distance = entry[3].ToSingle();

            tickAway = entry?.Count >= 5 &&
                       entry[4].ToString()!.Equals("tick away", StringComparison.InvariantCultureIgnoreCase);
        }

        Inputs.Add(new NodePin(this, LinkType.Condition, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Source Ship", ref sourceShip);
        Controls.InputTextId("Dest Object", ref destObject);

        ImGui.Checkbox("Inside", ref inside);
        Controls.HelpMarker(
            "Whether the source ship should be within (true) the specified distance, or if the condition is " +
            "triggered when the source ship is at least the specified distance away from the destination object.",
            true);

        ImGui.SliderFloat("Distance", ref distance, 0.0f, 100000.0f, "%.0f", ImGuiSliderFlags.AlwaysClamp);
        ImGui.Checkbox("Tick Away", ref tickAway);
    }
}
