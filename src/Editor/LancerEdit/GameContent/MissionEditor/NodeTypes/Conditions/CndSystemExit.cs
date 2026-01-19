using System;
using System.Collections.Generic;
using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions.Conditions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class CndSystemExit : NodeTriggerEntry
{
    public override string Name => "On System Exit";

    public Cnd_SystemExit Data;
    public CndSystemExit(Entry entry): base(NodeColours.Condition)
    {
        Data = entry is null ? new() : new(entry);

    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.CheckboxUndo("Any", undoBuffer, () => ref Data.Any);
        ImGui.BeginDisabled(Data.Any);
        Controls.InputStringList("Systems", undoBuffer, Data.Systems);
        ImGui.EndDisabled();
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
