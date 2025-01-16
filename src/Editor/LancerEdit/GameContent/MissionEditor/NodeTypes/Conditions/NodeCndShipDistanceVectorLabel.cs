using System;
using System.Numerics;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class NodeCndShipDistanceVectorLabel : BlueprintNode
{
    protected override string Name => "On Ship Distance Change (Label)";

    private string label;
    private bool inside;
    private Vector3 position;
    private float distance;
    private string sourceShip;
    private bool tickAway;

    public NodeCndShipDistanceVectorLabel(ref int id, Entry entry) : base(ref id, NodeColours.Condition)
    {
        if (entry?.Count >= 6)
        {
            inside = entry[0].ToString()!.Equals("inside", StringComparison.InvariantCultureIgnoreCase);
            sourceShip = entry[1].ToString();
            label = entry[2].ToString();
            position = new Vector3(entry[3].ToSingle(), entry[4].ToSingle(), entry[5].ToSingle());
            distance = entry[6].ToSingle();

            tickAway = entry?.Count >= 8 &&
                       entry[7].ToString()!.Equals("tick away", StringComparison.InvariantCultureIgnoreCase);
        }

        Inputs.Add(new NodePin(this, LinkType.Condition, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Source Ship", ref sourceShip);

        ImGui.Checkbox("Inside", ref inside);
        Controls.HelpMarker(
            "Whether the source ship should be within (true) the specified distance, or if the condition is " +
            "triggered when the source ship is at least the specified distance away from the destination object.",
            true);

        // TODO: Transform label into combo selection
        Controls.InputTextId("Label", ref label);
        ImGui.InputFloat3("Position", ref position, "%.0f");
        ImGui.SliderFloat("Radius", ref distance, 0.0f, 100000.0f, "%.0f", ImGuiSliderFlags.AlwaysClamp);
        ImGui.Checkbox("Tick Away", ref tickAway);
    }
}
