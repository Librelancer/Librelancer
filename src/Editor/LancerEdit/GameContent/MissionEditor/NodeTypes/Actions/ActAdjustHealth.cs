using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActAdjustHealth : NodeTriggerEntry
{
    public override string Name => "Adjust Health";

    public readonly Act_AdjHealth Data;

    public ActAdjustHealth(MissionAction action) : base(NodeColours.Action)
    {
        Data = action is null ? new Act_AdjHealth() : new Act_AdjHealth(action);
        ;

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.InputTextIdUndo("Target", undoBuffer, () => ref Data.Target);
        Controls.SliderFloatUndo(
            "Health",
            undoBuffer,
            () => ref Data.Adjustment,
            -1f,
            1f,
            "%.2f",
            ImGuiSliderFlags.AlwaysClamp);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }

    public override MissionCondition CloneCondition() => null;
    public override MissionAction CloneAction()
    {
        return new MissionAction(
            TriggerActions.Act_AdjHealth,
            BuildEntry()
        );
    }
}
