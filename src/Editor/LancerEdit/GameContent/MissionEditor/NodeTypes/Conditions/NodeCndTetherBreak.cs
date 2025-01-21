using System;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;
using LibreLancer.Missions.Conditions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class NodeCndTetherBreak : TriggerEntryNode
{
    protected override string Name => "On Tether Break";

    public Cnd_TetherBroke Data;
    public NodeCndTetherBreak(ref int id, Entry entry) : base(ref id, NodeColours.Condition)
    {
        Data = entry is null ? new() : new(entry);

        Inputs.Add(new NodePin(this, LinkType.Condition, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
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
