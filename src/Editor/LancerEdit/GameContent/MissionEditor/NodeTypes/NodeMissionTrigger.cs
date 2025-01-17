using System;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes;

public sealed class NodeMissionTrigger : BlueprintNode
{
    protected override string Name => "Mission Trigger";

    public readonly MissionTrigger Data;
    public NodeMissionTrigger(ref int id, MissionTrigger data) : base(ref id, NodeColours.Trigger)
    {
        this.Data = data ?? new MissionTrigger();

        Inputs.Add(new NodePin(this, LinkType.Trigger, PinKind.Input));
        Outputs.Add(new NodePin(this, LinkType.Action, PinKind.Output));
        Outputs.Add(new NodePin(this, LinkType.Condition, PinKind.Output));
    }

    private readonly string[] initStateOptions = Enum.GetNames<TriggerInitState>();
    protected override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        MissionIni missionIni)
    {
        Controls.InputTextId("ID", ref Data.Nickname);
        Controls.InputTextId("System", ref Data.System);
        ImGui.Checkbox("Repeatable", ref Data.Repeatable);

        var index = (int)Data.InitState;
        nodePopups.Combo("Initial State", index, i => index = i, initStateOptions);
        Data.InitState = (TriggerInitState)index;
    }
}
