using System.Linq;
using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

// ReSharper disable once InconsistentNaming
public sealed class ActSetNnObject : NodeTriggerEntry
{
    public override string Name => "Set NN Objective";

    public readonly Act_SetNNObj Data;
    public ActSetNnObject(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_SetNNObj(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        nodePopups.StringCombo("Objective", undoBuffer, () => ref Data.Objective, lookups.Objectives);
        Controls.CheckboxUndo("History", undoBuffer, () => ref Data.History);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
