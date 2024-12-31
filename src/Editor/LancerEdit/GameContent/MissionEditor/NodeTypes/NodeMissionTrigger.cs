using System;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes;

public sealed class NodeMissionTrigger : BlueprintNode
{
    protected override string Name => "Add Real-Time Cutscene";

    private readonly MissionTrigger data;
    public NodeMissionTrigger(ref int id, MissionTrigger data) : base(ref id, NodeColours.Action)
    {
        this.data = data;
    }

    private readonly string[] initStateOptions = Enum.GetNames<TriggerInitState>();
    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("ID", ref data.Nickname);
        Controls.InputTextId("System", ref data.System);
        ImGui.Checkbox("Repeatable", ref data.Repeatable);

        var index = (int)data.InitState;
        ImGui.Combo("Initial State", ref index, initStateOptions, initStateOptions.Length);
        data.InitState = (TriggerInitState)index;
    }
}
