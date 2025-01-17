using System;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class NodeCndProjectileHitShipToLabel : BlueprintNode
{
    protected override string Name => "On Projectile Hit (Label)";

    private string target = string.Empty;
    private int count = 1;
    private string source = string.Empty;

    public NodeCndProjectileHitShipToLabel(ref int id, Entry entry) : base(ref id, NodeColours.Condition)
    {
        if (entry?.Count >= 2)
        {
            target = entry[0].ToString();
            count = entry[1].ToInt32();
            if (entry?.Count >= 3)
            {
                source = entry[2].ToString();
            }
        }

        Inputs.Add(new NodePin(this, LinkType.Condition, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        MissionIni missionIni)
    {
        Controls.InputTextId("Source Label", ref source);
        Controls.InputTextId("Target", ref target);
        ImGui.InputInt("Count", ref count, 1, 100);
        count = Math.Clamp(count, 1, 10000);
    }
}
