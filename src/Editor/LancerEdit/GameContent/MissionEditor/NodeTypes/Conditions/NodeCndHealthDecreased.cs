using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class NodeCndHealthDecreased : BlueprintNode
{
    protected override string Name => "On Health Decreased";

    private string target = string.Empty;
    private float percent;

    public NodeCndHealthDecreased(ref int id, Entry entry) : base(ref id, NodeColours.Condition)
    {
        if (entry.Count >= 2)
        {
            target = entry[0].ToString();
            percent = entry[1].ToSingle();
        }

        Inputs.Add(new NodePin(id++, this, LinkType.Condition, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Target", ref target);
        ImGui.SliderFloat("Health", ref percent, 0, 1f, "%.2f", ImGuiSliderFlags.AlwaysClamp);
    }
}
