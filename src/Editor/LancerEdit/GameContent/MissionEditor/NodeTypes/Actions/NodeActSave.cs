using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActSave : BlueprintNode
{
    protected override string Name => "Save Game";

    public readonly Act_Save Data;

    public NodeActSave(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        Data = action is null ? new() : new Act_Save(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));

        Outputs.Add(new NodePin(this, LinkType.Trigger, PinKind.Output));
    }

    public override void OnLinkCreated(NodeLink link)
    {
        Data.Trigger = (link.EndPin.OwnerNode as NodeMissionTrigger)!.Data.Nickname;
    }

    public override void OnLinkRemoved(NodeLink link)
    {
        if (link.EndPin.OwnerNode == this)
        {
            Data.Trigger = string.Empty;
        }
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        MissionIni missionIni)
    {
        var text = string.IsNullOrWhiteSpace(Data.Trigger) ? "No Trigger" : Data.Trigger;

        ImGui.BeginDisabled();
        Controls.InputTextId("Trigger", ref text);
        ImGui.EndDisabled();

        Controls.IdsInputString("IDS", gameData, popup, ref Data.Ids, (ids) => Data.Ids = ids);
    }
}
