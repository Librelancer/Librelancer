using System.Linq;
using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActSetNnHidden : NodeTriggerEntry
{
    public override string Name => "Set NN Hidden";

    public readonly Act_SetNNHidden Data;
    public ActSetNnHidden(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_SetNNHidden(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        nodePopups.StringCombo("Objective", Data.Objective, s => Data.Objective = s, lookups.Objectives);
        ImGui.Checkbox("Hidden", ref Data.Hide);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
