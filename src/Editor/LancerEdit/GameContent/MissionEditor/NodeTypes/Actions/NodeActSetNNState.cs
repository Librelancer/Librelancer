using System.Linq;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActSetNNState : BlueprintNode
{
    protected override string Name => "Set NN State";

    public readonly Act_SetNNState Data;
    public NodeActSetNNState(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        Data = action is null ? new() : new Act_SetNNState(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        MissionIni missionIni)
    {
        var objectives = missionIni.Objectives.Select(x => x.Nickname).ToArray();
        nodePopups.StringCombo("Objective", Data.Objective, s => Data.Objective = s, objectives);
        ImGui.Checkbox("Complete", ref Data.Complete);
    }
}
