using System;
using System.Linq;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;

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
        var systems = gameData.GameData.Systems.Select(x => x.Nickname).Order().Order().ToArray();

        Controls.InputTextId("ID", ref Data.Nickname);
        nodePopups.StringCombo("System", Data.System, s => Data.System = s, systems, true);
        ImGui.Checkbox("Repeatable", ref Data.Repeatable);

        var index = (int)Data.InitState;
        nodePopups.Combo("Initial State", index, i => index = i, initStateOptions);
        Data.InitState = (TriggerInitState)index;
    }

    public void WriteNode(MissionScriptEditorTab missionEditor, IniBuilder builder)
    {
        var s = builder.Section("Trigger");

        var actions = missionEditor.GetLinkedNodes(this, PinKind.Output, LinkType.Action);
        var conditions = missionEditor.GetLinkedNodes(this, PinKind.Output, LinkType.Condition);
    }
}
