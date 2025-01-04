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

    private readonly Act_SetRep data;

    public NodeActSetRep(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_SetRep(action);

        Inputs.Add(new NodePin(id++, this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Object", ref data.Object);
        Controls.InputTextId("Faction", ref data.Faction);
        VibeComboBox(ref data.VibeSet);

        ImGui.BeginDisabled(data.VibeSet != VibeSet.None);
        ImGui.SliderFloat("Value", ref data.NewValue, -1, 1, "%.2f");
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
