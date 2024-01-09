using ImGuiNET;
using LancerEdit.GameContent.MissionEditor.NodeTypes;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.Registers;

internal static partial class Registers
{
    private static bool RegisterMissionFormationIo(Node node, ref int pinId)
    {
        node.Inputs.Add(new NodePin(pinId++, "Action: Spawn Formation", node, LinkType.MissionFormation, PinKind.Input));
        node.Inputs.Add(new NodePin(pinId++, "Mission Ship", node, LinkType.MissionShip, PinKind.Input));
        return true;
    }

    internal static void MissionFormationContent(GameDataContext context, MissionScript script, ref NodePopups popups, MissionFormation obj)
    {
        using var value = NodeValue.Begin(obj.Nickname);

        Controls.InputTextId("Nickname##ID", ref obj.Nickname);
        Controls.InputTextId("Formation##ID", ref obj.Formation);

        ImGui.InputFloat3("Position##ID", ref obj.Position);
        Controls.InputFlQuaternion("Orientation##ID", ref obj.Orientation);
    }
}
