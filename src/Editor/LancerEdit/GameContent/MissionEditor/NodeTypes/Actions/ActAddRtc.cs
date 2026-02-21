using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActAddRtc : NodeTriggerEntry
{
    public override string Name => "Add Real-Time Cutscene";

    public readonly Act_AddRTC Data;
    public ActAddRtc(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_AddRTC(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.InputTextUndo("RTC", undoBuffer, () => ref Data.RTC);
        Controls.CheckboxUndo("Repeatable", undoBuffer, () => ref Data.Repeatable);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }

    public override MissionCondition CloneCondition() => null;
    public override MissionAction CloneAction()
    {
        return new MissionAction(
            TriggerActions.Act_AddRTC,
            BuildEntry()
        );
    }
}
