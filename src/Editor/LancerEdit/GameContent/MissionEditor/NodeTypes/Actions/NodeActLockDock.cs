using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActLockDock : BlueprintNode
{
    protected override string Name => "Toggle Docking Lock";

    public readonly Act_LockDock Data;
    public NodeActLockDock(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        Data = action is null ? new() : new Act_LockDock(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Target", ref Data.Target);
        Controls.InputTextId("Object", ref Data.Object);
        ImGui.Checkbox("Lock", ref Data.Lock);
    }
}
