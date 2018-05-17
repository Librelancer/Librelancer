using System;
using LibreLancer;
using System.Runtime.InteropServices;
using ImGuiNET;
namespace LancerEdit
{
    public class ImGuiExt
    {
        [DllImport("cimgui", EntryPoint = "igBuildFontAtlas", CallingConvention = CallingConvention.Cdecl)]
        public static extern void BuildFontAtlas(IntPtr atlas);

        [DllImport("cimgui", EntryPoint = "igExtSplitterV", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SplitterV(float thickness, ref float size1, ref float size2, float min_size1, float min_size2, float splitter_long_axis_size);
        const string PADDING = "     ";
        public static string Pad(string s)
        {
            return PADDING + s;
        }

        public static bool ToggleButton(string text, bool v)
        {
            if (v) {
                var style = ImGui.GetStyle();
                ImGui.PushStyleColor(ColorTarget.Button, style.GetColor(ColorTarget.ButtonActive));
            }
            var retval = ImGui.Button(text);
            if(v) ImGui.PopStyleColor();
            return retval;
        }
	}
}
