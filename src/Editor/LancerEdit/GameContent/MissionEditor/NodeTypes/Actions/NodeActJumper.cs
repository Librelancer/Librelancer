using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActJumper : BlueprintNode
{
    protected override string Name => "Nag Greet";

    private readonly Act_Jumper data;
    public NodeActJumper(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_Jumper(action);

        Inputs.Add(new NodePin(id++, this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Target", ref data.Target);
        ImGui.Checkbox("Jump With Player", ref data.JumpWithPlayer);
    }
}
