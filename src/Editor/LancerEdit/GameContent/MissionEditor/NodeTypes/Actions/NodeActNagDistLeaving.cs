using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActNagDistLeaving : BlueprintNode
{
    protected override string Name => "Nag Dist Leaving";

    private int radioIndex = 0;
    private readonly Act_NagDistLeaving data;
    public NodeActNagDistLeaving(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_NagDistLeaving(action);

        Inputs.Add(new NodePin(id++, this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        ImGui.RadioButton("Point", ref radioIndex, 0);
        ImGui.RadioButton("Object", ref radioIndex, 1);

        Controls.InputTextId("Nickname", ref data.Nickname);
        Controls.InputTextId("Nagger", ref data.Nagger);
        Controls.IdsInputString("Mission Fail IDS", gameData, popup, ref data.MissionFailIds, (ids) => data.MissionFailIds = ids);
        ImGui.InputFloat("Distance", ref data.Distance);
        // ImGui.Combo("Nag Type");

        if (radioIndex == 0)
        {
            data.Target = "";
            ImGui.InputFloat3("Position", ref data.Position);
        }
        else
        {
            Controls.InputTextId("Target", ref data.Target);
        }
    }
}
