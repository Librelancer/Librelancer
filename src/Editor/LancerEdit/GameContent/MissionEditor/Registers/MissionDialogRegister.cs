using System;
using System.Collections.Generic;
using ImGuiNET;
using LancerEdit.GameContent.MissionEditor.NodeTypes;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor.Registers;

internal static partial class Registers
{
    private static bool RegisterDialogFormationIo(Node node, ref int pinId)
    {
        node.Inputs.Add(new NodePin(pinId++, "Action: Start Dialog", node, LinkType.Dialogue, PinKind.Input));
        return true;
    }

    internal static void MissionDialogContent(GameDataContext context, MissionScript script, ref NodeBuilder builder, MissionDialog obj)
    {
        using var value = NodeValue.Begin(obj.Nickname);

        Controls.InputTextId("Nickname##ID", ref obj.Nickname);
        Controls.InputTextId("System##ID", ref obj.System);

        for (var index = 0; index < obj.Lines.Count; index++)
        {
            var line = obj.Lines[index];
            ImGui.PushID(line.GetHashCode());
            Controls.InputTextId("Source##ID", ref line.Source);
            Controls.InputTextId("Target##ID", ref line.Target);
            Controls.InputTextId("Line##ID", ref line.Line);
            ImGui.PopID();

            if (index + 1 != obj.Lines.Count)
            {
                ImGui.Text("Then");
            }
        }

        if (ImGui.Button(Icons.PlusCircle))
        {
            obj.Lines.Add(new DialogLine());
            return;
        }

        ImGui.SameLine();
        ImGui.BeginDisabled(obj.Lines.Count is 0);
        if (ImGui.Button(Icons.X))
        {
            obj.Lines.RemoveAt(obj.Lines.Count - 1);
        }

        ImGui.EndDisabled();
    }
}
