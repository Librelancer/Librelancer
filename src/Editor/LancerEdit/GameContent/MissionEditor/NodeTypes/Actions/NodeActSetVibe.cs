using System;
using System.Linq;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActSetVibe : TriggerEntryNode
{
    protected override string Name => "Set Vibe";

    public readonly Act_SetVibe Data;

    public NodeActSetVibe(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        Data = action is null ? new() : new Act_SetVibe(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        MissionIni missionIni)
    {
        VibeComboBox(ref Data.Vibe, nodePopups);
        Controls.InputTextId("Target", ref Data.Target);
        Controls.InputTextId("Other", ref Data.Other);
    }

    private static readonly string[] _vibeList = Enum.GetValues<VibeSet>().Select(x => x.ToString()).ToArray();
    public static void VibeComboBox(ref VibeSet vibeSet, NodePopups nodePopups)
    {
        var index = (int)vibeSet;
        nodePopups.Combo("Vibe", index, (idx) => index = idx, _vibeList);
        vibeSet = (VibeSet)index;
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
