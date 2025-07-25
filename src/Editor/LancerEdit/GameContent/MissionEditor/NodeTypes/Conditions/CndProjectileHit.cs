using System;
using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions.Conditions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class CndProjectileHit : NodeTriggerEntry
{
    public override string Name => "On Projectile Hit";

    public Cnd_ProjHit Data;
    public CndProjectileHit(Entry entry): base(NodeColours.Condition)
    {
        Data = entry is null ? new() : new(entry);

    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.InputTextId("Target", ref Data.target);
        Controls.InputTextId("Source", ref Data.source);
        ImGui.InputInt("Count", ref Data.count, 1, 100);
        Data.count = Math.Clamp(Data.count, 1, 10000);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
