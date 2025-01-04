using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActSetPriority : BlueprintNode
{
    protected override string Name => "Set Priority";

    private readonly Act_SetPriority data;
    public NodeActSetPriority(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_SetPriority(action);

        Inputs.Add(new NodePin(id++, "Trigger", this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Object", ref data.Object);
        ImGui.Checkbox("Always Execute", ref data.AlwaysExecute);
    }
}
