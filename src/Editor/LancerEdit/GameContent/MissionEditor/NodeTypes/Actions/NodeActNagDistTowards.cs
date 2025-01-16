using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActNagDistTowards : BlueprintNode
{
    protected override string Name => "Nag Dist Leaving";

    private int radioIndex = 0;
    public readonly Act_NagDistTowards Data;
    public NodeActNagDistTowards(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        Data = action is null ? new() : new Act_NagDistTowards(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        ImGui.RadioButton("Point", ref radioIndex, 0);
        ImGui.RadioButton("Object", ref radioIndex, 1);

        Controls.InputTextId("Nickname", ref Data.Nickname);
        Controls.InputTextId("Nagger", ref Data.Nagger);
        Controls.IdsInputString("Mission Fail IDS", gameData, popup, ref Data.MissionFailIds, (ids) => Data.MissionFailIds = ids);
        ImGui.InputFloat("Distance", ref Data.Distance);
        // ImGui.Combo("Nag Type");

        if (radioIndex == 0)
        {
            Data.IsObject = true;
            ImGui.InputFloat3("Position", ref Data.Position);
        }
        else
        {
            Data.IsObject = false;
            Controls.InputTextId("Object", ref Data.Object);
        }
    }
}
