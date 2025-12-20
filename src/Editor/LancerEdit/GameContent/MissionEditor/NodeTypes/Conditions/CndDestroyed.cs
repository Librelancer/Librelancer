using System.Linq;
using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
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

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        nodePopups.StringCombo("Target", undoBuffer, () => ref Data.label, lookups.ShipsAndLabels);

        Controls.InputIntUndo("Count", undoBuffer, () => ref Data.Count);
        nodePopups.Combo("Kind", undoBuffer, () => ref Data.Kind);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
