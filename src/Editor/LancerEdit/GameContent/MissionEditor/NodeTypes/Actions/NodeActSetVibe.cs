using System;
using System.Linq;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActSetVibe : BlueprintNode
{
    protected override string Name => "Set Vibe";

    private readonly Act_SetVibe data;

    public NodeActSetVibe(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_SetVibe(action);
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        VibeComboBox(ref data.Vibe);
        Controls.InputTextId("Target", ref data.Target);
        Controls.InputTextId("Other", ref data.Other);
    }

    private static readonly string[] _vibeList = Enum.GetValues<VibeSet>().Select(x => x.ToString()).ToArray();
    public static void VibeComboBox(ref VibeSet vibeSet)
    {
        var index = (int)vibeSet;
        ImGui.Combo("Vibe", ref index, _vibeList, _vibeList.Length);
        vibeSet = (VibeSet)index;
    }
}
