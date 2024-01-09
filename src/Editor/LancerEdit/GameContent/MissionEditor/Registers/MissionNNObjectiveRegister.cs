using System;
using ImGuiNET;
using LancerEdit.GameContent.MissionEditor.NodeTypes;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Net;

namespace LancerEdit.GameContent.MissionEditor.Registers;

internal static partial class Registers
{
    // ReSharper disable twice InconsistentNaming
    private static bool RegisterMissionNNObjectiveIo(Node node, ref int pinId)
    {
        node.Inputs.Add(new NodePin(pinId++, "Action: Set NN Objective", node, LinkType.NNObjective, PinKind.Input));
        return true;
    }

    internal static void MissionNNObjectiveContent(GameDataContext context, MissionScript script, ref NodePopups popups,
        NNObjective obj)
    {
        using var value = NodeValue.Begin(obj.Nickname);

        Controls.InputTextId("Nickname##ID", ref obj.Nickname);
        Controls.InputTextId("State##ID", ref obj.State);

        var index = obj.TypeData.Type switch
        {
            "ids" => 0,
            "navmarker" => 1,
            "rep_inst" => 2,
            _ => throw new InvalidOperationException("Invalid NNObjective type provided: " + obj.Type[0])
        };

        var types = new[] { "IDS", "Nav Marker", "Rep Inst" };
        popups.Combo("Type##ID", index, i => obj.TypeData.Type = types[i], types);

        switch (index)
        {
            case 0:
                ImGui.InputInt("IDS##ID", ref obj.TypeData.NameIds);
                break;
            case 2:
                Controls.InputTextId("Solar Nickname##ID", ref obj.TypeData.SolarNickname);
                goto case 1;
            case 1:
                Controls.InputTextId("System##ID", ref obj.TypeData.System);
                ImGui.InputInt("Name IDS##ID", ref obj.TypeData.NameIds);
                ImGui.InputInt("Explanation##ID", ref obj.TypeData.ExplanationIds);
                ImGui.InputFloat3("Position##ID", ref obj.TypeData.Position);
                break;
        }

    }
}
