using System;
using ImGuiNET;
using LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;
using LibreLancer.Missions.Conditions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

// i am not convinced that watch vibe works in the base game, needs testing

public class CndWatchVibe : NodeTriggerEntry
{
    public override string Name => "On Vibe State Change (Reputation)";

    // How does the source feel about the target

    private Cnd_WatchVibe Data;
    public CndWatchVibe(Entry entry): base(NodeColours.Condition)
    {
        Data = entry is null ? new() : new Cnd_WatchVibe(entry);

    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.InputTextIdUndo("Source Object", undoBuffer, () => ref Data.SourceObject);
        Controls.InputTextIdUndo("Target Object", undoBuffer, () => ref Data.TargetObject);

        nodePopups.Combo("Vibe", undoBuffer, () => ref Data.Vibe);
        nodePopups.Combo("Operator", undoBuffer, () => ref Data.Operator);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }

    public override MissionCondition CloneCondition()
    {
        return new MissionCondition(
            TriggerConditions.Cnd_WatchVibe,
            BuildEntry()
        );
    }
    public override MissionAction CloneAction() => null;
}
