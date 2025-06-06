using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions.Conditions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class CndBaseEnter : NodeTriggerEntry
{
    public override string Name => "On Base Enter";

    public Cnd_BaseEnter Data;
    public CndBaseEnter(Entry entry): base(NodeColours.Condition)
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
    }
}
