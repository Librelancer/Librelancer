using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions.Conditions;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class CndTradeLaneExit : NodeTriggerEntry
{
    public override string Name => "On TL Exit";

    public Cnd_TLExited Data;
    public CndTradeLaneExit(Entry entry): base(NodeColours.Condition)
    {
        Data = entry is null ? new() : new(entry);
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.InputTextIdUndo("Source", undoBuffer, () => ref Data.Source);
        Controls.InputTextIdUndo("Start Ring", undoBuffer, () => ref Data.StartRing);
        Controls.InputTextIdUndo("Next Ring", undoBuffer, () => ref Data.NextRing);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
