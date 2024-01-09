using ImGuiNET;
using LancerEdit.GameContent.MissionEditor.NodeTypes;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.Registers;

internal static partial class Registers
{
    private static bool RegisterMissionSolarIo(Node node, ref int pinId)
    {
        node.Inputs.Add(new NodePin(pinId++, "Action: Spawn Solar", node, LinkType.MissionShip, PinKind.Input));
        return true;
    }

    internal static void MissionSolarContent(GameDataContext context, MissionScript script, ref NodePopups popups, MissionSolar solar)
    {
        using var value = NodeValue.Begin(solar.Nickname);

        Controls.InputTextId("Nickname##ID", ref solar.Nickname);
        Controls.InputTextId("Faction##ID", ref solar.Faction);
        Controls.InputTextId("System##ID", ref solar.System);
        Controls.InputTextId("Archetype##ID", ref solar.Archetype);
        Controls.InputTextId("Base##ID", ref solar.Base);
        Controls.InputTextId("Loadout##ID", ref solar.Loadout);
        Controls.InputTextId("Pilot##ID", ref solar.Pilot);
        Controls.InputTextId("Visit##ID", ref solar.Visit);

        ImGui.NewLine();
        Controls.InputTextId("Costume Head##ID", ref solar.Costume[0]);
        Controls.InputTextId("Costume Body##ID", ref solar.Costume[1]);
        Controls.InputTextId("Costume Comm##ID", ref solar.Costume[2]);
        Controls.InputTextId("Voice##ID", ref solar.Voice);

        ImGui.NewLine();
        Controls.InputStringList("Labels", solar.Labels);

        ImGui.InputFloat3("Position##ID", ref solar.Position);
        Controls.InputFlQuaternion("Orientation##ID", ref solar.Orientation);
        ImGui.InputFloat("Radius##ID", ref solar.Radius);
    }
}
