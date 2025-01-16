using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActCanTradeLane : BlueprintNode
{
    protected override string Name => "Toggle Player Docking (Tradelane) Ability";

    public readonly Act_PlayerCanTradelane Data;
    public NodeActCanTradeLane(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        Data = action is null ? new() : new Act_PlayerCanTradelane(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        ImGui.Checkbox("Can Dock", ref Data.CanDock);
        Controls.InputStringList("Exceptions", Data.Exceptions);
    }
}
