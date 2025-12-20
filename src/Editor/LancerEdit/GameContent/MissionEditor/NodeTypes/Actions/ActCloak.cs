using System.Linq;
using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActCloak : NodeTriggerEntry
{
    public override string Name => "Act Cloak";

    public readonly Act_Cloak Data;
    public ActCloak(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_Cloak(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        nodePopups.StringCombo("Ship", undoBuffer, () => ref Data.Target, lookups.Ships);
        Controls.CheckboxUndo("Cloak", undoBuffer, () => ref Data.Cloaked);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
