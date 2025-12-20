using System.Linq;
using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActSpawnFormation : NodeTriggerEntry
{
    public override string Name => "Spawn Formation";

    public readonly Act_SpawnFormation Data;
    public ActSpawnFormation(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_SpawnFormation(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        nodePopups.StringCombo("Formation", undoBuffer, () => ref Data.Formation, lookups.Formations);
        Controls.InputOptionalVector3Undo("Position", undoBuffer, () => ref Data.Position);
        ImGui.BeginDisabled(!Data.Position.Present);
        Controls.InputOptionalQuaternionUndo("Orientation", undoBuffer, () => ref Data.Orientation);
        ImGui.EndDisabled();
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
