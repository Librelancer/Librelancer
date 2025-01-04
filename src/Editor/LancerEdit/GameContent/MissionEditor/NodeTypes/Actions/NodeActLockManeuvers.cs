using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActLockManeuvers : BlueprintNode
{
    protected override string Name => "Lock Maneuvers";

    private readonly Act_LockManeuvers data;
    public NodeActLockManeuvers(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        data = new Act_LockManeuvers(action);

        Inputs.Add(new NodePin(id++, "Trigger", this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        ImGui.Checkbox("Lock", ref data.Lock);
    }
}
