using System.Linq;
using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActInvulnerable : NodeTriggerEntry
{
    public override string Name => "Set Invulnerable";

    public readonly Act_Invulnerable Data;
    public ActInvulnerable(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_Invulnerable(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        nodePopups.StringCombo("Object", undoBuffer, () => ref Data.Object, lookups.ShipsAndSolars);

        Controls.CheckboxUndo("Is Invulnerable", undoBuffer, () => ref Data.Invulnerable);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
