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

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        MissionIni missionIni)
    {
        Controls.InputTextId("ID", ref Data.Nickname);
        nodePopups.StringCombo("System", Data.System, s => Data.System = s, gameData.SystemsByName, true);
        ImGui.Checkbox("Repeatable", ref Data.Repeatable);

        nodePopups.Combo("Initial State", Data.InitState, x => Data.InitState = x);
    }

    public void WriteNode(MissionScriptEditorTab missionEditor, IniBuilder builder)
    {
        var s = builder.Section("Trigger");

        var actions = missionEditor.GetLinkedNodes(this, PinKind.Output, LinkType.Action).OfType<TriggerEntryNode>().ToArray();
        var conditions = missionEditor.GetLinkedNodes(this, PinKind.Output, LinkType.Condition).OfType<TriggerEntryNode>().ToArray();

        if (string.IsNullOrWhiteSpace(Data.Nickname))
        {
            return;
        }

        s.Entry("nickname", Data.Nickname);
        s.Entry("InitState", Data.InitState.ToString());
        if (Data.System != string.Empty)
        {
            s.Entry("system", Data.System);
        }

        s.Entry("repeatable", Data.Repeatable);

        foreach (var condition in conditions)
        {
            condition.WriteEntry(s);
        }

        foreach (var action in actions)
        {
            action.WriteEntry(s);
        }
    }
}
