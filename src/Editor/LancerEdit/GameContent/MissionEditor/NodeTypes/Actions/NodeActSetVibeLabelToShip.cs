using System;
using System.Linq;
using ImGuiNET;
using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActSetVibeLabelToShip : BlueprintNode
{
    protected override string Name => "Set Vibe Label to Ship";

    private readonly Act_SetVibeLblToShip data;

    public NodeActSetVibeLabelToShip(ref int id, Act_SetVibeLblToShip data) : base(ref id, NodeColours.Action)
    {
        this.data = data;
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionScript missionScript)
    {
        NodeActSetVibe.VibeComboBox(ref data.Vibe);
        Controls.InputTextId("Label", ref data.Label);
        Controls.InputTextId("Ship", ref data.Ship);
    }
}
