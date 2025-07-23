using System.Runtime.InteropServices;

namespace ImGuiNET;

public static unsafe partial class ImGui
{
    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ImGui_SeparatorEx")]
    public static extern void SeparatorEx(ImGuiSeparatorFlags flags, float thickness = 1.0f);
}
