using System.Linq;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActSetNnState : NodeTriggerEntry
{
    public override string Name => "Set NN State";

    public readonly Act_SetNNState Data;
    public ActSetNnState(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_SetNNState(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        nodePopups.StringCombo("Objective", Data.Objective, s => Data.Objective = s, lookups.Objectives);
        ImGui.Checkbox("Complete", ref Data.Complete);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
