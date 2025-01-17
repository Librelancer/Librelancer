using System.Linq;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActSetNNHidden : TriggerEntryNode
{
    protected override string Name => "Set NN Hidden";

    public readonly Act_SetNNHidden Data;
    public NodeActSetNNHidden(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        Data = action is null ? new() : new Act_SetNNHidden(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        MissionIni missionIni)
    {
        var objectives = missionIni.Objectives.Select(x => x.Nickname).Order().ToArray();
        nodePopups.StringCombo("Objective", Data.Objective, s => Data.Objective = s, objectives);
        ImGui.Checkbox("Hidden", ref Data.Hide);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
