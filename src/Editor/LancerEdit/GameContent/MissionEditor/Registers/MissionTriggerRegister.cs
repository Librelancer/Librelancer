using System.Linq;
using ImGuiNET;
using LancerEdit.GameContent.MissionEditor.NodeTypes;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.Registers;

internal static partial class Registers
{
    private static bool RegisterMissionTriggerIo(Node node, ref int pinId)
    {
        node.Inputs.Add(new NodePin(pinId++, "Action: Start Trigger", node, LinkType.Trigger, PinKind.Input));

        node.Outputs.Add(new NodePin(pinId++, "Actions", node, LinkType.Action, PinKind.Output));
        node.Outputs.Add(new NodePin(pinId++, "Conditions", node, LinkType.Condition, PinKind.Output));
        return true;
    }

    internal static void MissionTriggerContent(GameDataContext context, MissionScript script, ref NodePopups popups, MissionTrigger obj)
    {
        using var value = NodeValue.Begin(obj.Nickname);

        Controls.InputTextId("Nickname##ID", ref obj.Nickname);
        Controls.InputTextId("System##ID", ref obj.System);
        MissionEditorHelpers.AlertIfInvalidRef(() => obj.System.Length is 0 ||
                                                     context.GameData.Systems.Any(x => x.Nickname == obj.System));

        var initState = obj.InitState == TriggerInitState.ACTIVE;
        ImGui.Checkbox("Initial State##ID", ref initState);
        obj.InitState = initState ? TriggerInitState.ACTIVE : TriggerInitState.INACTIVE;

        ImGui.Checkbox("Repeatable##ID", ref obj.Repeatable);
    }
}
