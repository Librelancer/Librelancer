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

        const string PADDING = "     ";
        public static string Pad(string s)
        {
            return PADDING + s;
        }
	}
}
