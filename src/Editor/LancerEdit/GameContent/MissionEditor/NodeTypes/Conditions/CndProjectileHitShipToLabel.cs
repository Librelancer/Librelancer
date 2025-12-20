using System;
using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions.Conditions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class CndProjectileHitShipToLabel : NodeTriggerEntry
{
    public override string Name => "On Projectile Hit (Label)";

    public Cnd_ProjHitShipToLbl Data;
    public CndProjectileHitShipToLabel(Entry entry): base(NodeColours.Condition)
    {
        Data = entry is null ? new() : new(entry);

    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.InputTextIdUndo("Source Label", undoBuffer, () => ref Data.source);
        Controls.InputTextIdUndo("Target", undoBuffer, () => ref Data.target);
        Controls.InputIntUndo("Count", undoBuffer, () => ref Data.count, 1, 100, default, new(1, 10000));
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
