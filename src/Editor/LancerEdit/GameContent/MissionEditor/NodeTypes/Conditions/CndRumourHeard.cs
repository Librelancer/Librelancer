using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
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

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        ImGui.Text("This node has not been tested in game, and the values may be incorrect.");
        Controls.CheckboxUndo("Has Heard Rumour", undoBuffer, () => ref Data.HasHeardRumor);
        Controls.InputIntUndo("Rumour Id", undoBuffer, () => ref Data.RumorId);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
