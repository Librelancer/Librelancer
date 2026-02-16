using System;
using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions.Conditions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class CndShipDistance : NodeTriggerEntry
{
    public override string Name => "On Ship Distance Change (Object)";

    public Cnd_DistShip Data;

    public CndShipDistance(Entry entry): base(NodeColours.Condition)
    {
        Data = entry is null ? new() : new(entry);

    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.InputTextIdUndo("Source Ship", undoBuffer, () => ref Data.SourceShip);
        Controls.InputTextIdUndo("Dest Object", undoBuffer, () => ref Data.DestObject);

        Controls.CheckboxUndo("Inside", undoBuffer, () => ref Data.Inside);
        Controls.HelpMarker(
            "Whether the source ship should be within (true) the specified distance, or if the condition is " +
            "triggered when the source ship is at least the specified distance away from the destination object.",
            true);

        Controls.InputFloatUndo("Distance", undoBuffer, () => ref Data.Distance);
        Controls.InputOptionalFloatUndo("Tick Away", undoBuffer, () => ref Data.TickAway);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
