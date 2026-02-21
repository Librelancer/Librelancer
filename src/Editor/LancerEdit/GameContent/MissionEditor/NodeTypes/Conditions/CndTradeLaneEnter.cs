using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions.Conditions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class CndTradeLaneEnter : NodeTriggerEntry
{
    public override string Name => "On TL Enter";

    public Cnd_TLEntered Data;
    public CndTradeLaneEnter(Entry entry): base(NodeColours.Condition)
    {
        Data = entry is null ? new() : new(entry);

    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.InputTextIdUndo("Source Ship", undoBuffer, () => ref Data.Source);
        Controls.InputTextIdUndo("Start Ring", undoBuffer, () => ref Data.StartRing);
        Controls.InputTextIdUndo("Next Ring", undoBuffer, () => ref Data.NextRing);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }

    public override MissionCondition CloneCondition()
    {
        return new MissionCondition(
            TriggerConditions.Cnd_TLEntered,
            BuildEntry()
        );
    }
    public override MissionAction CloneAction() => null;
}
