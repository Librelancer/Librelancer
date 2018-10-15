/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2018
 * the Initial Developer. All Rights Reserved.
 */
using System;
using LibreLancer;
using System.Runtime.InteropServices;
using ImGuiNET;
namespace LibreLancer.ImUI
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

        const int ImDrawCornerFlags_All = 0xF;
        public static unsafe void ToastText(string text, Color4 background, Color4 foreground)
        {
            var displaySize = (Vector2)(ImGui.GetIO().DisplaySize);
            var textSize = (Vector2)ImGui.GetTextSize(text);
            var drawlist = ImGuiNative.igGetOverlayDrawList();
            var textbytes = System.Text.Encoding.UTF8.GetBytes(text);
            ImGuiNative.ImDrawList_AddRectFilled(
                drawlist,
                new Vector2(displaySize.X - textSize.X - 9, 2),
                new Vector2(displaySize.X, textSize.Y + 9),
                GetUint(background), 2, ImDrawCornerFlags_All
            );
            fixed (byte* ptr = textbytes)
            {
                ImGuiNative.ImDrawList_AddText(
                    drawlist, 
                    new Vector2(displaySize.X - textSize.X - 3,2), 
                    GetUint(foreground), ptr, 
                    (byte*)0
                );
            }
        }

        public static unsafe uint GetUint(Color4 color)
        {
            uint a = 0;
            var ptr = (byte*)&a;
            ptr[0] = (byte)(color.R * 255);
            ptr[1] = (byte)(color.G * 255);
            ptr[2] = (byte)(color.B * 255);
            ptr[3] = (byte)(color.A * 255);
            return a;
        }
        [DllImport("cimgui", EntryPoint = "igExtSpinner", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool Spinner(string label, float radius, int thickness, uint color);
	}
}
