using System;
using System.Linq;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActSetRep : BlueprintNode
{
    protected override string Name => "Set Reputation";

    public readonly Act_SetRep Data;

    public NodeActSetRep(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        Data = action is null ? new() : new Act_SetRep(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Object", ref Data.Object);
        Controls.InputTextId("Faction", ref Data.Faction);
        VibeComboBox(ref Data.VibeSet);

        ImGui.BeginDisabled(Data.VibeSet != VibeSet.None);
        ImGui.SliderFloat("Value", ref Data.NewValue, -1, 1, "%.2f");
        ImGui.EndDisabled();
    }

    private static readonly string[] _vibeList = Enum.GetValues<VibeSet>().Select(x => x.ToString()).ToArray();
    public static void VibeComboBox(ref VibeSet vibeSet)
    {
        var index = (int)vibeSet;
        ImGui.Combo("Vibe", ref index, _vibeList, _vibeList.Length);
        vibeSet = (VibeSet)index;
    }
}
