using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions.Conditions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class CndCharacterSelect : NodeTriggerEntry
{
    public override string Name => "On Character Select";

    public Cnd_CharSelect Data;
    public CndCharacterSelect(Entry entry): base(NodeColours.Condition)
    {
        Data = entry is null ? new() : new(entry);

    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.InputTextIdUndo("Character", undoBuffer, () => ref Data.Character);
        Controls.InputTextIdUndo("Location", undoBuffer, () => ref Data.Room);
        Controls.InputTextIdUndo("Base", undoBuffer, () => ref Data.Base);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
