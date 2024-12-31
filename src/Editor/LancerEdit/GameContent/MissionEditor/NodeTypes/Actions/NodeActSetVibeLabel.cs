using System;
using System.Linq;
using ImGuiNET;
using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActSetVibeLabel : BlueprintNode
{
    protected override string Name => "Set Vibe Label";

    private readonly Act_SetVibeLbl data;

    public NodeActSetVibeLabel(ref int id, Act_SetVibeLbl data) : base(ref id, NodeColours.Action)
    {
        this.data = data;
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionScript missionScript)
    {
        NodeActSetVibe.VibeComboBox(ref data.Vibe);
        Controls.InputTextId("Label 1", ref data.Label1);
        Controls.InputTextId("Label 2", ref data.Label2);
    }
}
