using System;
using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActSetLifetime : NodeTriggerEntry
{
    public override string Name => "Set Lifetime";

    public readonly Act_SetLifetime Data;
    public ActSetLifetime(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_SetLifetime(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.InputTextIdUndo("Object", undoBuffer, () => ref Data.Object);
        Controls.InputIntUndo("Seconds", undoBuffer, () => ref Data.Seconds, 1, 10, default, new(0, 100000));
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
