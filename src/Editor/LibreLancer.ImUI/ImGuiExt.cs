// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
namespace LibreLancer.ImUI
{
    public unsafe class ImGuiExt
    {
        [DllImport("cimgui", CallingConvention =  CallingConvention.Cdecl)]
        internal static extern void igFtLoad();

        [DllImport("cimgui", EntryPoint = "igExtSplitterV", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SplitterV(float thickness, ref float size1, ref float size2, float min_size1, float min_size2, float splitter_long_axis_size);

        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr igExtGetVersion();

        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        static extern bool igExtComboButton(IntPtr idstr, IntPtr preview_value);

        [DllImport("cimgui", EntryPoint = "igSeparatorEx", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SeparatorEx(int flags, float thickness); //not bound in imgui.net ?

        public static readonly string Version;

        static ImGuiExt()
        {
            Version = Marshal.PtrToStringUTF8(igExtGetVersion());
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ImFontGlyph
        {
            public ushort Codepoint;
            public float AdvanceX;
            public float X0, Y0, X1, Y1;
            public float U0, V0, U1, V1;
        }

        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern ImFontGlyph* igFontFindGlyph(ImFont* font, uint c);

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

        public static unsafe bool ComboButton(string id, string preview)
        {
            byte* native_id;
            int id_byteCount = 0;
            if (id != null)
            {
                id_byteCount = Encoding.UTF8.GetByteCount(id);
                if (id_byteCount > Util.StackAllocationSizeLimit)
                {
                    native_id = Util.Allocate(id_byteCount + 1);
                }
                else
                {
                    byte* native_id_stackBytes = stackalloc byte[id_byteCount + 1];
                    native_id = native_id_stackBytes;
                }
                int native_id_offset = Util.GetUtf8(id, native_id, id_byteCount);
                native_id[native_id_offset] = 0;
            }
            else { native_id = null; }
            byte* native_preview;
            int preview_byteCount = 0;
            if (preview != null)
            {
                preview_byteCount = Encoding.UTF8.GetByteCount(preview);
                if (preview_byteCount > Util.StackAllocationSizeLimit)
                {
                    native_preview = Util.Allocate(preview_byteCount + 1);
                }
                else
                {
                    byte* native_preview_stackBytes = stackalloc byte[preview_byteCount + 1];
                    native_preview = native_preview_stackBytes;
                }
                int native_preview_offset = Util.GetUtf8(preview, native_preview, preview_byteCount);
                native_preview[native_preview_offset] = 0;
            }
            else { native_preview = null; }
            var retval = igExtComboButton((IntPtr)native_id, (IntPtr)native_preview);
            if (id_byteCount > Util.StackAllocationSizeLimit)
                Util.Free(native_id);
            if (preview_byteCount > Util.StackAllocationSizeLimit)
                Util.Free(native_preview);
            return retval;
        }
        public static bool ToggleButton(string text, bool v, bool enabled = true)
        {
            ImGui.BeginDisabled(!enabled);
            if (v) {
                var style = ImGui.GetStyle();
                ImGui.PushStyleColor(ImGuiCol.Button, style.Colors[(int)ImGuiCol.ButtonActive]);
            }
            var retval = ImGui.Button(text);
            if(v) ImGui.PopStyleColor();
            ImGui.EndDisabled();
            return retval;
        }

        /// <summary>
        /// Button that can be disabled
        /// </summary>
        /// <param name="text">Text.</param>
        /// <param name="enabled">If set to <c>true</c> enabled.</param>
        public static bool Button(string text, bool enabled)
        {
           ImGui.BeginDisabled(!enabled);
           var r = ImGui.Button(text);
           ImGui.EndDisabled();
           return r;
        }

        /// <summary>
        /// Button that can be disabled
        /// </summary>
        /// <param name="icon">Icon.</param>
        /// <param name="enabled">If set to <c>true</c> enabled.</param>
        public static bool Button(char icon, bool enabled)
        {
            Span<byte> str = stackalloc byte[5];
            Span<char> c = stackalloc char[1];
            c[0] = icon;
            int l = Encoding.UTF8.GetBytes(c, str);
            str[l] = 0;
            ImGui.BeginDisabled(!enabled);
            bool r;
            fixed(byte* b = str)
                r = ImGuiNative.igButton(b, new Vector2()) != 0;
            ImGui.EndDisabled();
            return r;
        }

        public static void Checkbox(string label, ref bool v, bool enabled, string disableReason)
        {
            ImGui.BeginDisabled(!enabled);
            ImGui.Checkbox(label, ref v);
            ImGui.EndDisabled();
            if (!enabled && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            {
                ImGui.BeginTooltip();
                ImGui.Text(disableReason);
                ImGui.EndTooltip();
            }
        }

        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        public static bool ColorPicker3(string label, ref Color4 color, float size = -1f)
        {
            if(size != -1f) ImGui.PushItemWidth(size);
            var v3 = new Vector3(color.R, color.G, color.B);
            var retval = ImGui.ColorPicker3(label, ref v3);
            color.R = v3.X;
            color.G = v3.Y;
            color.B = v3.Z;
            if(size != -1f) ImGui.PopItemWidth();
            return retval;
        }

        public static unsafe void ToastText(string text, Color4 background, Color4 foreground)
        {
            var displaySize = ImGui.GetIO().DisplaySize;
            var textSize = ImGui.CalcTextSize(text);
            var drawlist = ImGui.GetForegroundDrawList();
            drawlist.AddRectFilled(new Vector2(displaySize.X - textSize.X - 9, 2),
                new Vector2(displaySize.X, textSize.Y + 9),
                GetUint(background), 2, ImDrawFlags.RoundCornersAll);
            drawlist.AddText(new Vector2(displaySize.X - textSize.X - 3, 2),
                GetUint(foreground), text);
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

        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        static extern bool igExtSeparatorText(IntPtr text);

        public static void SeparatorText(string text)
        {
            byte* native_name;
            int name_byteCount = 0;
            if (text != null)
            {
                name_byteCount = Encoding.UTF8.GetByteCount(text);
                if (name_byteCount > Util.StackAllocationSizeLimit)
                {
                    native_name = Util.Allocate(name_byteCount + 1);
                }
                else
                {
                    byte* native_name_stackBytes = stackalloc byte[name_byteCount + 1];
                    native_name = native_name_stackBytes;
                }
                int native_name_offset = Util.GetUtf8(text, native_name, name_byteCount);
                native_name[native_name_offset] = 0;
            }
            else { native_name = null; }
            igExtSeparatorText((IntPtr)native_name);
            if (name_byteCount > Util.StackAllocationSizeLimit)
            {
                Util.Free(native_name);
            }
        }
	}
}
