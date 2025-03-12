using System;
using System.Numerics;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;
using LibreLancer.Missions.Conditions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class CndShipDistanceVectorLabel : NodeTriggerEntry
{
    public override string Name => "On Ship Distance Change (Label)";

    public Cnd_DistVecLbl Data;
    public CndShipDistanceVectorLabel(Entry entry): base(NodeColours.Condition)
    {
        Data = entry is null ? new() : new(entry);

    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        ImGui.Checkbox("Any", ref Data.Any);

        ImGui.Checkbox("Inside", ref Data.Inside);
        Controls.HelpMarker(
            "Whether the source ship should be within (true) the specified distance, or if the condition is " +
            "triggered when the source ship is at least the specified distance away from the destination object.",
            true);

        // TODO: Transform label into combo selection
        Controls.InputTextId("Label", ref Data.Label);
        ImGui.InputFloat3("Position", ref Data.Position, "%.0f");
        ImGui.SliderFloat("Radius", ref Data.Distance, 0.0f, 100000.0f, "%.0f", ImGuiSliderFlags.AlwaysClamp);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
