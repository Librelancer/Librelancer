using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;
using LibreLancer.Missions.Conditions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class CndBaseExit : NodeTriggerEntry
{
    public override string Name => "On Base Exit";

    public Cnd_BaseExit Data;
    public CndBaseExit(Entry entry): base(NodeColours.Condition)
    {
        Data = entry is null ? new() : new(entry);
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.InputTextId("Base", ref Data.@base); // TODO: Comboify
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
