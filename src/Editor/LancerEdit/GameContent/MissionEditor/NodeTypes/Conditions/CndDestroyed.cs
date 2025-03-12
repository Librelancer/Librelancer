using System.Linq;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;
using LibreLancer.Missions.Conditions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class CndDestroyed : NodeTriggerEntry
{
    public override string Name => "On Object Destroyed";

    public Cnd_Destroyed Data;

    public CndDestroyed(Entry entry): base(NodeColours.Condition)
    {
        Data = entry is null ? new() : new(entry);

    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        nodePopups.StringCombo("Target", Data.label, s => Data.label = s, lookups.ShipsAndLabels);

        ImGui.InputInt("Count", ref Data.Count);
        nodePopups.Combo("Kind", Data.Kind, x => Data.Kind = x);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
