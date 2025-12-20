using System;
using System.Linq;
using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActSetRep : NodeTriggerEntry
{
    public override string Name => "Set Reputation";

    public readonly Act_SetRep Data;

    public ActSetRep(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_SetRep(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {

        Controls.InputTextIdUndo("Object", undoBuffer, () => ref Data.Object);
        nodePopups.StringCombo("Faction", undoBuffer, () => ref Data.Faction, gameData.FactionsByName);
        nodePopups.Combo("Vibe", undoBuffer, () => ref Data.VibeSet);

        ImGui.BeginDisabled(Data.VibeSet != VibeSet.None);
        Controls.SliderFloatUndo("Value", undoBuffer, () => ref Data.NewValue, -1, 1, "%.2f");
        ImGui.EndDisabled();
    }


    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
