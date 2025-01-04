using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActLockDock : BlueprintNode
{
    protected override string Name => "Toggle Docking Lock";

    private readonly Act_LockDock data;
    public NodeActLockDock(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_LockDock(action);

        Inputs.Add(new NodePin(id++, "Trigger", this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Target", ref data.Target);
        Controls.InputTextId("Object", ref data.Object);
        ImGui.Checkbox("Lock", ref data.Lock);
    }
}
