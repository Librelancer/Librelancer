using System;
using System.Linq;
using ImGuiNET;
using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActSetVibeShipToLabel : BlueprintNode
{
    protected override string Name => "Set Vibe Ship to Label";

    private readonly Act_SetVibeShipToLbl data;

    public NodeActSetVibeShipToLabel(ref int id, Act_SetVibeShipToLbl data) : base(ref id, NodeColours.Action)
    {
        this.data = data;
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionScript missionScript)
    {
        NodeActSetVibe.VibeComboBox(ref data.Vibe);
        Controls.InputTextId("Ship", ref data.Ship);
        Controls.InputTextId("Label", ref data.Label);
    }
}
