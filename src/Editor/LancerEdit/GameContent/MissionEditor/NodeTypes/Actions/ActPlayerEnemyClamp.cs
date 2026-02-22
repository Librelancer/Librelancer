using System;
using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActPlayerEnemyClamp : NodeTriggerEntry
{
    public override string Name => "Clamp Amount of Enemies Attacking Player";

    public readonly Act_PlayerEnemyClamp Data;
    public ActPlayerEnemyClamp(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_PlayerEnemyClamp(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.InputIntUndo("Min", undoBuffer, () => ref Data.Min, 1, 10, default, new(0, Data.Max));
        Controls.InputIntUndo("Max", undoBuffer, () => ref Data.Max, 1, 10, default, new(Data.Min, 100));
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }

    public override MissionCondition CloneCondition() => null;
    public override MissionAction CloneAction()
    {
        return new MissionAction(
            TriggerActions.Act_PlayerEnemyClamp,
            BuildEntry()
        );
    }
}
