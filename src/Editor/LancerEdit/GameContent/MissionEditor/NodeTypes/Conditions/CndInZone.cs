using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions.Conditions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class CndInZone : NodeTriggerEntry
{
    public override string Name => "In Zone";

    private Cnd_InZone Data;
    public CndInZone(Entry entry): base(NodeColours.Condition)
    {
        Data = entry is null ? new() : new(entry);
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.CheckboxUndo("In Zone", undoBuffer, () => ref Data.InZone);
        Controls.InputTextIdUndo("Ship", undoBuffer, () => ref Data.Ship); // TODO: Swap out for combo
        Controls.InputTextIdUndo("Zone", undoBuffer, () => ref Data.Zone);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }

    public override MissionCondition CloneCondition()
    {
        return new MissionCondition(
            TriggerConditions.Cnd_InZone,
            BuildEntry()
        );
    }
    public override MissionAction CloneAction() => null;
}
