using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActSetPriority : NodeTriggerEntry
{
    public override string Name => "Set Priority";

    public readonly Act_SetPriority Data;
    public ActSetPriority(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_SetPriority(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.InputTextIdUndo("Object", undoBuffer, () => ref Data.Object);
        Controls.CheckboxUndo("Always Execute", undoBuffer, () => ref Data.AlwaysExecute);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
