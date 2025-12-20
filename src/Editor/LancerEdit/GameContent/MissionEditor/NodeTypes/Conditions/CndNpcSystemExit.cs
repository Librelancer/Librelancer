using System;
using System.Collections.Generic;
using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions.Conditions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;
public class CndNpcSystemExit : NodeTriggerEntry
{
    public override string Name => "On NPC System Exit";

    public Cnd_NPCSystemExit Data;
    public CndNpcSystemExit(Entry entry): base(NodeColours.Condition)
    {
        Data = entry is null ? new() : new(entry);

    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        ImGui.Text("This node type has not been tested. Proceed with caution.");
        Controls.InputTextIdUndo("System", undoBuffer, () => ref Data.system);
        Controls.InputStringList("Ships", undoBuffer, Data.ships);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
