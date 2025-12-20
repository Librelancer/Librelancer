using System.Linq;
using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActNnPath : NodeTriggerEntry
{
    public override string Name => "Set NN Path";

    public readonly Act_NNPath Data;
    public ActNnPath(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_NNPath(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.InputTextIdUndo("Object", undoBuffer, () => ref Data.ObjectId);
        nodePopups.StringCombo("System", undoBuffer, () => ref Data.SystemId, gameData.SystemsByName);
        Controls.IdsInputStringUndo("IDS 1", gameData, popup, undoBuffer, () => ref Data.Ids1);
        Controls.IdsInputStringUndo("IDS 2", gameData, popup, undoBuffer, () => ref Data.Ids2);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
