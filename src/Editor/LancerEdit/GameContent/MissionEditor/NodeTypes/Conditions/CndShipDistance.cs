using System;
using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions.Conditions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class CndShipDistance : NodeTriggerEntry
{
    public override string Name => "On Ship Distance Change (Object)";

    public Cnd_DistShip Data;

    public CndShipDistance(Entry entry): base(NodeColours.Condition)
    {
        Data = entry is null ? new() : new(entry);

    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.InputTextId("Source Ship", ref Data.sourceShip);
        Controls.InputTextId("Dest Object", ref Data.destObject);

        ImGui.Checkbox("Inside", ref Data.inside);
        Controls.HelpMarker(
            "Whether the source ship should be within (true) the specified distance, or if the condition is " +
            "triggered when the source ship is at least the specified distance away from the destination object.",
            true);

        ImGui.SliderFloat("Distance", ref Data.distance, 0.0f, 100000.0f, "%.0f", ImGuiSliderFlags.AlwaysClamp);

        bool isTickAway = Data.tickAway != null;
        ImGui.Checkbox("Tick Away", ref isTickAway);
        if (isTickAway)
        {
            float value = Data.tickAway ?? 0;
            ImGui.InputFloat("Tick Away", ref value);
            Data.tickAway = value;
        }
        else
        {
            Data.tickAway = null;
        }
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
