using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActStaticCamera : NodeTriggerEntry
{
    public override string Name => "Set Static Camera";

    public readonly Act_StaticCam Data;
    public ActStaticCamera(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_StaticCam(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.InputFloat3Undo("Position", undoBuffer, () => ref Data.Position);
        Controls.InputQuaternionUndo("Orientation", undoBuffer, () => ref Data.Orientation);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
