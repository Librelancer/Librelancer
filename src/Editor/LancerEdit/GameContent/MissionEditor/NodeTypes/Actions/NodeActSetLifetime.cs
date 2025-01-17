using System;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActSetLifetime : BlueprintNode
{
    protected override string Name => "Set Lifetime";

    public readonly Act_SetLifetime Data;
    public NodeActSetLifetime(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        Data = action is null ? new() : new Act_SetLifetime(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        MissionIni missionIni)
    {
        Controls.InputTextId("Object", ref Data.Object);
        ImGui.InputInt("Seconds", ref Data.Seconds, 1, 10);

        Data.Seconds = Math.Clamp(Data.Seconds, 0, 100000);
    }
}
