using System;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActPlayerEnemyClamp : BlueprintNode
{
    protected override string Name => "Clamp Amount of Enemies Attacking Player";

    private readonly Act_PlayerEnemyClamp data;
    public NodeActPlayerEnemyClamp(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_PlayerEnemyClamp(action);

        Inputs.Add(new NodePin(id++, this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        ImGui.InputInt("Min", ref data.Min, 1, 10);
        ImGui.InputInt("Max", ref data.Max, 1, 10);

        data.Min = Math.Clamp(data.Min, 0, data.Max);
        data.Max = Math.Clamp(data.Max, data.Min, 100);
    }
}
