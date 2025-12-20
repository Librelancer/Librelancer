using System;
using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions.Conditions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class CndTetherBreak : NodeTriggerEntry
{
    public override string Name => "On Tether Break";

    public Cnd_TetherBroke Data;
    public CndTetherBreak(Entry entry): base(NodeColours.Condition)
    {
        Data = entry is null ? new() : new(entry);

    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.InputTextIdUndo("Source Ship", undoBuffer, () => ref Data.SourceShip);
        Controls.InputTextIdUndo("Dest Ship", undoBuffer, () => ref Data.DestShip);

        Controls.SliderFloatUndo("Distance", undoBuffer, () => ref Data.Distance, 0.0f, 100000.0f, "%.0f", ImGuiSliderFlags.AlwaysClamp);
        Controls.InputIntUndo("Count", undoBuffer, () => ref Data.Count, 1, 10, default, new(1, 300));

        Controls.SliderFloatUndo("Unknown", undoBuffer, () => ref Data.Unknown, 0.0f, 100000.0f, "%.0f", ImGuiSliderFlags.AlwaysClamp);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
