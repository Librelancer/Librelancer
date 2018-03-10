using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ImGuiNET;
namespace LancerEdit
{
	public class ImGuiExt
	{
        [DllImport("cimgui", EntryPoint = "igBuildFontAtlas", CallingConvention = CallingConvention.Cdecl)]
        public static extern void BuildFontAtlas(IntPtr atlas);
	}
}
