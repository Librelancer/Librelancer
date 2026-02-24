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

public sealed class ActSetVibe : NodeTriggerEntry
{
    public override string Name => "Set Vibe";

    public readonly Act_SetVibe Data;

    public ActSetVibe(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_SetVibe(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        nodePopups.Combo("Vibe", undoBuffer, () => ref Data.Vibe);
        Controls.InputTextIdUndo("Target", undoBuffer, () => ref Data.Target);
        Controls.InputTextIdUndo("Other", undoBuffer, () => ref Data.Other);
    }


    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }

    public override MissionCondition CloneCondition() => null;
    public override MissionAction CloneAction()
    {
        return new MissionAction(
            TriggerActions.Act_SetVibe,
            BuildEntry()
        );
    }
}
