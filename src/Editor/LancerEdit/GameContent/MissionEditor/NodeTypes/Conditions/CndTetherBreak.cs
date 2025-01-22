using System;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;
using LibreLancer.Missions.Conditions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class CndTetherBreak : NodeTriggerEntry
{
    public override string Name => "On Tether Break";

    public Cnd_TetherBroke Data;
    public CndTetherBreak(Entry entry): base(NodeColours.Condition)
    {
        Data = entry is null ? new() : new(entry);

    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.InputTextId("Source Ship", ref Data.SourceShip);
        Controls.InputTextId("Dest Ship", ref Data.DestShip);

        ImGui.SliderFloat("Distance", ref Data.Distance, 0.0f, 100000.0f, "%.0f", ImGuiSliderFlags.AlwaysClamp);
        ImGui.InputInt("Count", ref Data.Count, 1, 10);
        Data.Count = Math.Clamp(Data.Count, 1, 300);

        ImGui.SliderFloat("Unknown", ref Data.Unknown, 0.0f, 100000.0f, "%.0f", ImGuiSliderFlags.AlwaysClamp);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
