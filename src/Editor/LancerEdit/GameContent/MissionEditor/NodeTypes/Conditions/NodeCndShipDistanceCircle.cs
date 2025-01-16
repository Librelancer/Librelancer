using System.Numerics;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class NodeCndShipDistanceCircle : BlueprintNode
{
    protected override string Name => "On Ship Distance Change (Circle)";

    private string sourceShip;
    private string destObject;

    public NodeCndShipDistanceCircle(ref int id, Entry entry) : base(ref id, NodeColours.Condition)
    {
        if (entry?.Count >= 2)
        {
            sourceShip = entry[0].ToString();
            destObject = entry[1].ToString();
        }

        Inputs.Add(new NodePin(this, LinkType.Condition, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Source Ship", ref sourceShip);
        Controls.InputTextId("Dest Object", ref destObject);
    }
}
