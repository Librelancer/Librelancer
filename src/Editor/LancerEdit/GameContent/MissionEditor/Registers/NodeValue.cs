using System;
using System.Diagnostics.CodeAnalysis;
using ImGuiNET;

namespace LancerEdit.GameContent.MissionEditor.Registers;

[SuppressMessage("Performance", "CA1822:Mark members as static")]
public sealed class NodeValue : IDisposable
{
    private string id;
    private NodeValue()
    {
    }

    public static NodeValue Begin(string id)
    {
        ImGui.PushID(id);

        var value = new NodeValue();
        value.id = id;

        return value;
    }

    public void Dispose()
    {
        ImGui.PopID();
    }
}
