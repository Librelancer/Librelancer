using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActChangeState : NodeTriggerEntry
{
    public override string Name => "Change State";

    public readonly Act_ChangeState Data;
    public ActChangeState(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_ChangeState(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.CheckboxUndo("Success", undoBuffer, () => ref Data.Succeed);
        ImGui.BeginDisabled(Data.Succeed);
        Controls.IdsInputStringUndo("IDS", gameData, popup, undoBuffer, () => ref Data.Ids);
        ImGui.EndDisabled();
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }

    public override MissionCondition CloneCondition() => null;
    public override MissionAction CloneAction()
    {
        return new MissionAction(
            TriggerActions.Act_ChangeState,
            BuildEntry()
        );
    }
}
