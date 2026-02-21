using System.Linq;
using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions.Conditions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class CndInTradeLane : NodeTriggerEntry
{
    public override string Name => "In Trade Lane";

    private Cnd_InTradelane Data;
    public CndInTradeLane(Entry entry): base(NodeColours.Condition)
    {
        Data = entry is null ? new() : new(entry);

    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.CheckboxUndo("In Trade Lane", undoBuffer, () => ref Data.InTL);
        nodePopups.StringCombo("Ship", undoBuffer, () => ref Data.Ship, lookups.Ships);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }

    public override MissionCondition CloneCondition()
    {
        return new MissionCondition(
            TriggerConditions.Cnd_InTradelane,
            BuildEntry()
        );
    }
    public override MissionAction CloneAction() => null;
}
