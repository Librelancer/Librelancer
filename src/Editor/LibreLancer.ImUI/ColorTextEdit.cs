// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.InteropServices;
using ImGuiNET;

namespace LibreLancer.ImUI
{
    public enum ColorTextEditMode
    {
        Normal,
        Lua
    }
    public class ColorTextEdit : IDisposable
    {
        [DllImport("cimgui")]
        static extern IntPtr igExtTextEditorInit();

        [DllImport("cimgui")]
        static extern IntPtr igExtTextEditorGetText(IntPtr textedit);

        [DllImport("cimgui")]
        static extern void igExtFree(IntPtr mem);

        [DllImport("cimgui")]
        static extern void igExtTextEditorSetText(IntPtr textedit, IntPtr text);

        [DllImport("cimgui")]
        static extern void igExtTextEditorGetCoordinates(IntPtr textedit, out int x, out int y);

        [DllImport("cimgui")]
        static extern void igExtTextEditorFree(IntPtr textedit);

        [DllImport("cimgui")]
        static extern void igExtTextEditorRender(IntPtr textedit, IntPtr id);

        [DllImport("cimgui")]
        static extern bool igExtTextEditorIsTextChanged(IntPtr textedit);

        [DllImport("cimgui")]
        static extern void igExtTextEditorSetMode(IntPtr textedit, ColorTextEditMode mode);

        [DllImport("cimgui")]
        static extern void igExtTextEditorSetReadOnly(IntPtr textedit, bool readOnly);
        
        private IntPtr textedit;

        public ColorTextEdit()
        {
            textedit = igExtTextEditorInit();
        }

        public void SetText(string text)
        {
            var ptr = UnsafeHelpers.StringToHGlobalUTF8(text);
            igExtTextEditorSetText(textedit, ptr);
            Marshal.FreeHGlobal(ptr);
        }

        public string GetText()
        {
            var ptr = igExtTextEditorGetText(textedit);
            var str = UnsafeHelpers.PtrToStringUTF8(ptr);
            igExtFree(ptr);
            return str;
        }

        public void Render(string id)
        {
            ImGui.PushFont(ImGuiHelper.SystemMonospace);
            var ptr = UnsafeHelpers.StringToHGlobalUTF8(id);
            igExtTextEditorRender(textedit, ptr);
            Marshal.FreeHGlobal(ptr);
            ImGui.PopFont();
        }

        public void SetMode(ColorTextEditMode mode)
        {
            igExtTextEditorSetMode(textedit, mode);
        }

        public void SetReadOnly(bool readOnly)
        {
            igExtTextEditorSetReadOnly(textedit, readOnly);
        }
        public Point GetCoordinates()
        {
            igExtTextEditorGetCoordinates(textedit, out int x, out int y);
            return new Point(x,y);
        }

        public bool TextChanged()
        {
            return igExtTextEditorIsTextChanged(textedit);
        }
        
        public void Dispose()
        {
            if(textedit == IntPtr.Zero)
                throw new ObjectDisposedException("ColorTextEdit");
            igExtTextEditorFree(textedit);
            textedit = IntPtr.Zero;
        }
    }
}