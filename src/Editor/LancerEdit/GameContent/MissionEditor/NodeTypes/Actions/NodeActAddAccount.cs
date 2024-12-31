using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActAdjustAccount : BlueprintNode
{
    protected override string Name => "Adjust Account";

    private readonly Act_AdjAcct data;
    public NodeActAdjustAccount(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_AdjAcct(action);

        Inputs.Add(new NodePin(id++, "Trigger", this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        ImGui.InputInt("Amount", ref data.Amount);
    }
}
