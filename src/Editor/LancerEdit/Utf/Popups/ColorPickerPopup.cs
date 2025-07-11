using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.ImUI;

namespace LancerEdit.Utf.Popups;

public class ColorPickerPopup : PopupWindow
{
    public override string Title { get; set; } = "Color Picker";
    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize;

    Vector3 colorValue = Vector3.One;
    LUtfNode node;

    static void Fix(ref float f)
    {
        if (!float.IsFinite(f) ||
            !float.IsPositive(f) ||
            float.IsNaN(f))
            f = 0.0f;
        if (f > 1.0f)
            f = 1.0f;
    }

    public ColorPickerPopup(LUtfNode node)
    {
        this.node = node;
        if (node.Data != null && node.Data.Length >= 12)
        {
            colorValue = MemoryMarshal.Read<Vector3>(node.Data);
            Fix(ref colorValue.X);
            Fix(ref colorValue.Y);
            Fix(ref colorValue.Z);
        }
    }

    public override void Draw(bool appearing)
    {
        ImGui.ColorPicker3("Color", ref colorValue);
        if (ImGui.Button("Ok"))
        {
            node.Children = null;
            node.Data = UnsafeHelpers.CastArray([colorValue]);
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel"))
        {
            ImGui.CloseCurrentPopup();
        }
    }
}
