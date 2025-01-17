using System;
using System.Numerics;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class NodeCndShipDistanceVector : BlueprintNode
{
    protected override string Name => "On Ship Distance Change (Position)";

    private bool inside;
    private Vector3 position;
    private float distance;
    private string sourceShip;
    private bool tickAway;

    public NodeCndShipDistanceVector(ref int id, Entry entry) : base(ref id, NodeColours.Condition)
    {
        if (entry?.Count >= 6)
        {
            inside = entry[0].ToString()!.Equals("inside", StringComparison.InvariantCultureIgnoreCase);
            sourceShip = entry[1].ToString();
            position = new Vector3(entry[2].ToSingle(), entry[3].ToSingle(), entry[4].ToSingle());
            distance = entry[5].ToSingle();

            tickAway = entry?.Count >= 7 &&
                       entry[6].ToString()!.Equals("tick away", StringComparison.InvariantCultureIgnoreCase);
        }

        Inputs.Add(new NodePin(this, LinkType.Condition, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        MissionIni missionIni)
    {
        Controls.InputTextId("Source Ship", ref sourceShip);

        ImGui.Checkbox("Inside", ref inside);
        Controls.HelpMarker(
            "Whether the source ship should be within (true) the specified distance, or if the condition is " +
            "triggered when the source ship is at least the specified distance away from the destination object.",
            true);

        ImGui.InputFloat3("Position", ref position, "%.0f");
        ImGui.SliderFloat("Distance", ref distance, 0.0f, 100000.0f, "%.0f", ImGuiSliderFlags.AlwaysClamp);
        ImGui.Checkbox("Tick Away", ref tickAway);
    }
}
