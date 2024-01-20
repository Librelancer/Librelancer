using ImGuiNET;
using LancerEdit.GameContent.MissionEditor.NodeTypes;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.Registers;

internal static partial class Registers
{
    private static bool RegisterMissionShipIo(Node node, ref int pinId)
    {
        node.Inputs.Add(new NodePin(pinId++, "Action: Spawn Ship", node, LinkType.MissionShip, PinKind.Input));

        node.Outputs.Add(new NodePin(pinId++, "Command List", node, LinkType.CommandList, PinKind.Output));
        return true;
    }

    internal static void MissionShipContent(GameDataContext context, MissionScript script, ref NodePopups popups, MissionShip ship)
    {
        using var value = NodeValue.Begin(ship.Nickname);

        Controls.InputTextId("Nickname##ID", ref ship.Nickname);

        ImGui.InputFloat3("Position##ID", ref ship.Position);
        Controls.InputFlQuaternion("Orientation##ID", ref ship.Orientation);

        ImGui.InputFloat("Radius##ID", ref ship.Radius);
        ImGui.Checkbox("Is Jumper##ID", ref ship.Jumper);
        Controls.InputTextId("Arrival Object##ID", ref ship.ArrivalObj);

        ImGui.NewLine();
        popups.StringCombo("NPCs", ship.NPC, x => ship.NPC = x, script.NPCs.Keys);
    }
}
