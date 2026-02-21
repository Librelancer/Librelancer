using System;
using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions.Conditions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class CndProjectileHit : NodeTriggerEntry
{
    public override string Name => "On Projectile Hit";

    public Cnd_ProjHit Data;
    public CndProjectileHit(Entry entry): base(NodeColours.Condition)
    {
        Data = entry is null ? new() : new(entry);

    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.InputTextIdUndo("Target", undoBuffer, () => ref Data.Target);
        Controls.InputTextIdUndo("Source", undoBuffer, () => ref Data.Source);
        Controls.InputIntUndo("Count", undoBuffer, () => ref Data.Count, 1, 100, default, new(1, 10000));
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }

    public override MissionCondition CloneCondition()
    {
        return new MissionCondition(
            TriggerConditions.Cnd_ProjHit,
            BuildEntry()
        );
    }
    public override MissionAction CloneAction() => null;
}
