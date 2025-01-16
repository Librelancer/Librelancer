using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class NodeCndDestroyed : BlueprintNode
{
    protected override string Name => "On Object Destroyed";

    private string label = string.Empty;
    // TODO: INI has an extra two elements, one int, one enum. Figure out what they do
    public NodeCndDestroyed(ref int id, Entry entry) : base(ref id, NodeColours.Condition)
    {
        if (entry?.Count >= 1)
        {
            label = entry[0].ToString();
        }

        Inputs.Add(new NodePin(this, LinkType.Condition, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Target", ref label);
    }
}
