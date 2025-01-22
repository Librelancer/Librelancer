using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;
using LibreLancer.Missions.Conditions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class CndRumourHeard : NodeTriggerEntry
{
    public override string Name => "Has Rumour Been Heard";

    public Cnd_RumorHeard Data;
    public CndRumourHeard(Entry entry): base(NodeColours.Condition)
    {
        Data = entry is null ? new() : new(entry);

    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        ImGui.Text("This node has not been tested in game, and the values may be incorrect.");
        ImGui.Checkbox("Has Heard Rumour", ref Data.hasHeardRumour);
        ImGui.InputInt("Rumour Id", ref Data.rumourId);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
