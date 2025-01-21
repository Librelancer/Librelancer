using System;
using System.Linq;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;
using LibreLancer.Missions;
using LibreLancer.Missions.Conditions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class NodeCndWatchTrigger : TriggerEntryNode
{
    protected override string Name => "On Trigger State Change";

    public readonly Cnd_WatchTrigger Data;
    public NodeCndWatchTrigger(ref int id, Entry entry) : base(ref id, NodeColours.Condition)
    {
        Data = entry is null ? new() : new(entry);

        Inputs.Add(new NodePin(this, LinkType.Condition, PinKind.Input));
        Outputs.Add(new NodePin(this, LinkType.Trigger, PinKind.Output));
    }

    private readonly string[] triggerStates = Enum.GetNames<TriggerState>().ToArray();
    protected override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        nodePopups.Combo("Trigger State", (int)Data.TriggerState, i => Data.TriggerState = (TriggerState)i, triggerStates);
        Controls.HelpMarker(
            "If true then the conditional will only be successful if the linked trigger is set to active.",
            true);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {

    }

    public override void OnLinkCreated(NodeLink link)
    {
        if (link.StartPin.OwnerNode == this)
        {
            Data.Trigger = (link.EndPin.OwnerNode as NodeMissionTrigger)!.Data.Nickname;
        }
    }

    public override void OnLinkRemoved(NodeLink link)
    {
        if (link.EndPin.OwnerNode == this)
        {
            Data.Trigger = string.Empty;
        }
    }
}
