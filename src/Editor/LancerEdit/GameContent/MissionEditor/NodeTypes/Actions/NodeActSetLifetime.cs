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

    private readonly Act_SetLifetime data;
    public NodeActSetLifetime(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_SetLifetime(action);

        Inputs.Add(new NodePin(id++, this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Object", ref data.Object);
        ImGui.InputInt("Seconds", ref data.Seconds, 1, 10);

        data.Seconds = Math.Clamp(data.Seconds, 0, 100000);
    }
}
