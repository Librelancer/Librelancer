using ImGuiNET;
using LancerEdit.GameContent.MissionEditor.NodeTypes;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.Registers;

internal static partial class Registers
{
    private static bool RegisterMissionLootIo(Node node, ref int pinId)
    {
        node.Inputs.Add(new NodePin(pinId++, "Action: Spawn Loot", node, LinkType.MissionLoot, PinKind.Input));
        return true;
    }

    internal static void MissionLootContent(GameDataContext context, MissionScript script, ref NodeBuilder builder,
        MissionLoot loot)
    {
        using var value = NodeValue.Begin(loot.Nickname);

        Controls.InputTextId("Nickname##ID", ref loot.Nickname);
        Controls.InputTextId("Archetype##ID", ref loot.Archetype);
        ImGui.InputFloat("Health##ID", ref loot.Health);
        ImGui.Checkbox("Can Jettison##ID", ref loot.CanJettison);
        ImGui.InputInt("Equip Amount##ID", ref loot.EquipAmount);

        ImGui.NewLine();
        ImGui.InputFloat3("Position##ID", ref loot.Position);
        ImGui.InputFloat3("Velocity##ID", ref loot.Velocity);

        Controls.InputTextId("Relative Position Object##ID", ref loot.RelPosObj);

        Controls.InputTextId("Rel Pos X##ID", ref loot.RelPosOffset[0]);
        Controls.InputTextId("Rel Pos Y##ID", ref loot.RelPosOffset[1]);
        Controls.InputTextId("Rel Pos Z##ID", ref loot.RelPosOffset[2]);
    }
}
