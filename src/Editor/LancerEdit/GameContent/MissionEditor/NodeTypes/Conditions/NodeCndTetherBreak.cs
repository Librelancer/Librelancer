using System;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class NodeCndTetherBreak : BlueprintNode
{
    protected override string Name => "On Tether Break";

    private string sourceShip;
    private string destShip;
    private float distance;
    private int count;
    private float unknown;

    public NodeCndTetherBreak(ref int id, Entry entry) : base(ref id, NodeColours.Condition)
    {
        if (entry.Count >= 4)
        {
            sourceShip = entry[0].ToString()!;
            destShip = entry[1].ToString();
            distance = entry[2].ToSingle();
            count = entry[3].ToInt32();
            unknown = entry.Count >= 5 ? entry[4].ToSingle() : 0.0f;
        }

        Inputs.Add(new NodePin(id++, this, LinkType.Condition, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Source Ship", ref sourceShip);
        Controls.InputTextId("Dest Ship", ref destShip);

        ImGui.SliderFloat("Distance", ref distance, 0.0f, 100000.0f, "%.0f", ImGuiSliderFlags.AlwaysClamp);
        ImGui.InputInt("Count", ref count, 1, 10);
        count = Math.Clamp(count, 1, 300);

        ImGui.SliderFloat("Unknown", ref unknown, 0.0f, 100000.0f, "%.0f", ImGuiSliderFlags.AlwaysClamp);
    }
}
