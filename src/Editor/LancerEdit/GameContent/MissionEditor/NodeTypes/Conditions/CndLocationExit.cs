using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions.Conditions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class CndLocationExit : NodeTriggerEntry
{
    public override string Name => "On Location Exit";

    public Cnd_LocExit Data;
    public CndLocationExit(Entry entry): base(NodeColours.Condition)
    {
        Data = entry is null ? new() : new(entry);

    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.InputTextIdUndo("Location", undoBuffer, () => ref Data.Location);
        Controls.InputTextIdUndo("Base", undoBuffer, () => ref Data.Base);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }

    public override MissionCondition CloneCondition()
    {
        return new MissionCondition(
            TriggerConditions.Cnd_LocExit,
            BuildEntry()
        );
    }
    public override MissionAction CloneAction() => null;
}
