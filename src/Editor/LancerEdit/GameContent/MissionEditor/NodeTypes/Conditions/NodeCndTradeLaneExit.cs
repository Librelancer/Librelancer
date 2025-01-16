using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class NodeCndTradeLaneExit : BlueprintNode
{
    protected override string Name => "On TL Exit";

    private string startRing = string.Empty;
    private string nextRing = string.Empty;
    private string source = string.Empty;

    public NodeCndTradeLaneExit(ref int id, Entry entry) : base(ref id, NodeColours.Condition)
    {
        if (entry?.Count >= 2)
        {
            source = entry[0].ToString();
            startRing = entry[1].ToString();

            if (entry?.Count >= 3)
            {
                nextRing = entry[2].ToString();
            }
        }

        Inputs.Add(new NodePin(this, LinkType.Condition, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Source", ref source);
        Controls.InputTextId("Start Ring", ref startRing);
        Controls.InputTextId("Next Ring", ref nextRing);
    }
}
