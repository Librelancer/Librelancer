// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
namespace LibreLancer.ImUI
{
    public static unsafe class ImGuiExt
    {

        [DllImport("cimgui", EntryPoint = "igExtSplitterV", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SplitterV(float thickness, ref float size1, ref float size2, float min_size1, float min_size2, float splitter_long_axis_size);

        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr igExtGetVersion();

        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        static extern bool igExtComboButton(IntPtr idstr, IntPtr preview_value);

        [DllImport("cimgui", EntryPoint = "igExtUseTitlebar", CallingConvention = CallingConvention.Cdecl)]
        public static extern void UseTitlebar(out float restoreX, out float restoreY);

        [DllImport("cimgui")]
        static extern int igExtInputIntPreview(IntPtr label, IntPtr preview, ref int v);

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
        static extern void igExtDrawListAddTriangleMesh(IntPtr drawlist, IntPtr vertices, int count, uint color);

        public static void AddTriangleMesh(this ImDrawListPtr drawList, Vector2[] vertices, int count, VertexDiffuse color)
        {
            fixed (Vector2* ptr = vertices)
            {
                igExtDrawListAddTriangleMesh((IntPtr)drawList.Handle, (IntPtr)ptr, count, color);
            }
        }

        public static unsafe bool BeginModalNoClose(string name, ImGuiWindowFlags flags)
        {
            Span<byte> nbytes = stackalloc byte[512];
            using var native_name = new UTF8ZHelper(nbytes, name);
            fixed (byte* p = native_name.ToUTF8Z())
                return ImGuiNative.ImGui_BeginPopupModal(p, (byte*)0, flags) != 0;
        }

        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern void igExtRenderArrow(float x, float y);

        public static unsafe bool ComboButton(string id, string preview)
        {
            Span<byte> nbytes = stackalloc byte[512];
            using var native_id = new UTF8ZHelper(nbytes, id);

            Span<byte> pbytes = stackalloc byte[512];
            using var native_preview = new UTF8ZHelper(pbytes, preview);

            fixed(byte* ni = native_id.ToUTF8Z(), np = native_preview.ToUTF8Z())
                return igExtComboButton((IntPtr)ni, (IntPtr)np);
        }

        public static unsafe bool InputIntPreview(string id, string preview, ref int value)
        {
            Span<byte> nbytes = stackalloc byte[512];
            using var native_id = new UTF8ZHelper(nbytes, id);

            Span<byte> pbytes = stackalloc byte[512];
            using var native_preview = new UTF8ZHelper(pbytes, preview);
            fixed (byte* ni = native_id.ToUTF8Z(), np = native_preview.ToUTF8Z())
                return igExtInputIntPreview((IntPtr)ni, (IntPtr)np, ref value) != 0;
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
        /// <param name="text">Text.</param>
        /// <param name="enabled">If set to <c>true</c> enabled.</param>
        /// <param name="rounding">Corners to be rounded</param>
        public static bool Button(string text, bool enabled, ImDrawFlags rounding)
        {
            ImGui.BeginDisabled(!enabled);
            var r = ButtonRounding(text, Vector2.Zero, rounding);
            ImGui.EndDisabled();
            return r;
        }


        /// <summary>
        /// Button that can be disabled
        /// </summary>
        /// <param name="text">Text.</param>
        /// <param name="enabled">If set to <c>true</c> enabled.</param>
        public static bool Button(string text, bool enabled, Vector2 size = default)
        {
            ImGui.BeginDisabled(!enabled);
            var r = ImGui.Button(text, size);
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
                r = ImGuiNative.ImGui_Button(b, new Vector2()) != 0;
            ImGui.EndDisabled();
            return r;
        }


        [DllImport("cimgui")]
        static extern unsafe byte igButtonEx2(byte* label, float sizeX, float sizeY, int drawFlags);

        public static unsafe void ButtonDivided(string id, string label1, string label2, ref bool isOne)
        {
            ImGui.PushID(id);
            Span<byte> bytes1 = stackalloc byte[512];
            Span<byte> bytes2 = stackalloc byte[512];
            using var z1 = new UTF8ZHelper(bytes1, label1);
            using var z2 = new UTF8ZHelper(bytes2, label2);
            var wasOne = isOne;
            var style = ImGui.GetStyle();
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
            fixed (byte* a = z1.ToUTF8Z()) {
                if (wasOne)
                    ImGui.PushStyleColor(ImGuiCol.Button, style.Colors[(int)ImGuiCol.ButtonActive]);
                if (igButtonEx2(a, 0, 0, (int)ImDrawFlags.RoundCornersLeft) != 0)
                {
                    isOne = true;
                }
                if (wasOne)
                    ImGui.PopStyleColor();
            }
            ImGui.SameLine();
            ImGui.PopStyleVar();
            fixed (byte* b = z2.ToUTF8Z()) {
                if (!wasOne)
                    ImGui.PushStyleColor(ImGuiCol.Button, style.Colors[(int)ImGuiCol.ButtonActive]);
                if (igButtonEx2(b, 0, 0, (int)ImDrawFlags.RoundCornersRight) != 0)
                    isOne = false;
                if (!wasOne)
                    ImGui.PopStyleColor();
            }
            ImGui.PopID();
        }

        public static bool ButtonRounding(string label, Vector2 size, ImDrawFlags flags)
        {
            Span<byte> bytes = stackalloc byte[512];
            using var z1 = new UTF8ZHelper(bytes, label);
            fixed (byte* a = z1.ToUTF8Z())
                return igButtonEx2(a, size.X, size.Y, (int)flags) != 0;
        }

        public static void Checkbox(string label, ref bool v, bool enabled, string disableReason)
        {
            ImGui.BeginDisabled(!enabled);
            if (!enabled)
            {
                bool falseVal = false;
                ImGui.Checkbox(label, ref falseVal);
            }
            else
            {
                ImGui.Checkbox(label, ref v);
            }

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

        public static bool DropdownButton(string text)
        {
            text = $"{text}  ";
            var textSize = ImGui.CalcTextSize(text);
            var cpos = ImGui.GetCursorPosX();
            var cposY = ImGui.GetCursorPosY();
            var clicked = ImGui.Button(text);
            var style = ImGui.GetStyle();
            var tPos = new Vector2(cpos, cposY) + new Vector2(textSize.X + style.FramePadding.X, textSize.Y);
            Theme.TinyTriangle(tPos.X, tPos.Y);
            return clicked;
        }

        public static void DropdownButton(string id, ref int selected, IReadOnlyList<DropdownOption> options)
        {
            ImGui.PushID(id);
            if (DropdownButton($"{options[selected].Icon}  {options[selected].Name}"))
                ImGui.OpenPopup(id + "#popup");
            if (ImGui.BeginPopup(id + "#popup"))
            {
                ImGui.MenuItem(id, false);
                for (int i = 0; i < options.Count; i++)
                {
                    var opt = options[i];
                    if (Theme.IconMenuItem(opt.Icon, opt.Name, true))
                        selected = i;
                }
                ImGui.EndPopup();
            }
            ImGui.PopID();
        }

        [DllImport("cimgui")]
        static extern IntPtr ImGuiEx_GetOriginalInputTextString();
        [DllImport("cimgui")]
        static extern void ImGuiEx_FreeOriginalInputTextString(IntPtr str);

        private static IntPtr ogStringPtr;
        static unsafe int FetchString(ImGuiInputTextCallbackData* data)
        {
            callbackCalled = true;
            ogStringPtr = ImGuiEx_GetOriginalInputTextString();
            if (data->EventFlag == ImGuiInputTextFlags.CallbackCharFilter)
            {
                var ch = (char)data->EventChar;
                if ((ch >= '0' && ch <= '9') ||
                    (ch >= 'a' && ch <= 'z') ||
                    (ch >= 'A' && ch <= 'Z') ||
                    ch == '_')
                {
                    return 0;
                }
                if (ch == ' ')
                {
                    data->EventChar = (byte)'_';
                    return 0;
                }
                return 1;
            }
            return 0;
        }
        private static ImGuiInputTextCallback undoCb = FetchString;
        private static bool callbackCalled = false;

        public static bool InputTextLogged(string id, ref string buf, int bufSize, Action<string,string> onChanged, bool inputId = false)
        {
            callbackCalled = false;
            ogStringPtr = IntPtr.Zero;
            var flags = ImGuiInputTextFlags.CallbackAlways;
            if (inputId)
                flags |= ImGuiInputTextFlags.CallbackCharFilter;
            ImGui.InputText(id, ref buf, (uint)bufSize, flags, undoCb);
            string originalString = null;
            if (ogStringPtr != IntPtr.Zero)
            {
                originalString = Marshal.PtrToStringUTF8(ogStringPtr);
                ImGuiEx_FreeOriginalInputTextString(ogStringPtr);
            }
            if (ImGui.IsItemDeactivatedAfterEdit() &&
                originalString != buf)
            {
                onChanged(originalString, buf);
            }
            return callbackCalled;
        }


        public static void CenterText(string text)
        {
            ImGui.Dummy(new Vector2(1));
            var win = ImGui.GetWindowWidth();
            var txt = ImGui.CalcTextSize(text).X;
            ImGui.SameLine(Math.Max((win / 2f) - (txt / 2f), 0));
            ImGui.Text(text);
        }
        public static void CenterText(string text, Vector4 colour)
        {
            ImGui.Dummy(new Vector2(1));
            var win = ImGui.GetWindowWidth();
            var txt = ImGui.CalcTextSize(text).X;
            ImGui.SameLine(Math.Max((win / 2f) - (txt / 2f), 0));
            ImGui.TextColored(colour, text);

        }
    }
}
