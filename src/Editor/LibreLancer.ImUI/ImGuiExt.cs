// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Text;
using System.Numerics;
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
        public static unsafe bool BeginModalNoClose(string name, ImGuiWindowFlags flags)
        {
            byte* native_name;
            int name_byteCount = 0;
            if (name != null)
            {
                name_byteCount = Encoding.UTF8.GetByteCount(name);
                if (name_byteCount > Util.StackAllocationSizeLimit)
                {
                    native_name = Util.Allocate(name_byteCount + 1);
                }
                else
                {
                    byte* native_name_stackBytes = stackalloc byte[name_byteCount + 1];
                    native_name = native_name_stackBytes;
                }
                int native_name_offset = Util.GetUtf8(name, native_name, name_byteCount);
                native_name[native_name_offset] = 0;
            }
            else { native_name = null; }
            byte ret = ImGuiNative.igBeginPopupModal(native_name, (byte*)0, flags);
            if (name_byteCount > Util.StackAllocationSizeLimit)
            {
                Util.Free(native_name);
            }
            return ret != 0;
        }
        public static bool ToggleButton(string text, bool v)
        {
            if (v) {
                var style = ImGui.GetStyle();
                ImGui.PushStyleColor(ImGuiCol.Button, style.Colors[(int)ImGuiCol.ButtonActive]);
            }
            var retval = ImGui.Button(text);
            if(v) ImGui.PopStyleColor();
            return retval;
        }

        /// <summary>
        /// Button that can be disabled
        /// </summary>
        /// <param name="text">Text.</param>
        /// <param name="enabled">If set to <c>true</c> enabled.</param>
        public static bool Button(string text, bool enabled)
        {
            if (!enabled)
            {
                var style = ImGui.GetStyle();
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, style.Colors[(int)ImGuiCol.Button]);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, style.Colors[(int)ImGuiCol.Button]);
                ImGui.PushStyleColor(ImGuiCol.Text, style.Colors[(int)ImGuiCol.TextDisabled]);
                ImGui.Button(text);
                ImGui.PopStyleColor();
                ImGui.PopStyleColor();
                ImGui.PopStyleColor();
                return false;
            }
            else
                return ImGui.Button(text);
        }

        public static unsafe void ToastText(string text, Color4 background, Color4 foreground)
        {
            var displaySize = ImGui.GetIO().DisplaySize;
            var textSize = ImGui.CalcTextSize(text);
            var drawlist = ImGuiNative.igGetForegroundDrawList();
            var textbytes = System.Text.Encoding.UTF8.GetBytes(text);
            ImGuiNative.ImDrawList_AddRectFilled(
                drawlist,
                new Vector2(displaySize.X - textSize.X - 9, 2),
                new Vector2(displaySize.X, textSize.Y + 9),
                GetUint(background), 2, ImDrawCornerFlags.All
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

        public const char ReplacementHash = '\uE884';
        public static string IDSafe(string str)
        {
            if (str.IndexOf('#') == -1) return str;
            return str.Replace('#', ReplacementHash);
        }
        public static string IDWithExtra(string str, string extra) => string.Format("{0}##{1}", IDSafe(str), extra);
        public static string IDWithExtra(string str, object extra) => IDWithExtra(str, extra.ToString());

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

        public static bool Spinner(string label, float radius, int thickness, uint color)
        {
            ImGuiHelper.AnimatingElement();
            return _Spinner(label, radius, thickness, color);
        }
        
        [DllImport("cimgui", EntryPoint = "igExtSpinner", CallingConvention = CallingConvention.Cdecl)]
        static extern bool _Spinner(string label, float radius, int thickness, uint color);
	}
}
