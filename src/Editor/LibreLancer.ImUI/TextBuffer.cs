// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Text;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using LibreLancer;
namespace LibreLancer.ImUI
{
	public unsafe class TextBuffer : IDisposable
	{
		public int Size;
		public IntPtr Pointer;
		public ImGuiInputTextCallback Callback;
        public TextBuffer(int sz = 2048)
        {
            sz = (sz + 7) & ~7;
            Size = sz;
			Pointer = Marshal.AllocHGlobal(sz);
			Clear();
			Callback = HandleTextEditCallback;
		}

		public void Clear()
		{
			for (int i = 0; i < Size / sizeof(long); i++)
			{
				var ptr = (long*)Pointer;
				ptr[i] = 0;
			}
		}

		int HandleTextEditCallback(ImGuiInputTextCallbackData* data)
		{
			return 0;
		}

		public void SetText(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text).AsSpan();
            if (bytes.Length > Size - 1)
                bytes = bytes.Slice(bytes.Length - (Size - 1), (Size - 1));
            var dest = new Span<byte>((void*)Pointer, bytes.Length + 1);
            bytes.CopyTo(dest);
            dest[^1] = 0;
		}

        public unsafe void InputText(string id, ImGuiInputTextFlags flags, int sz = -1)
        {
            ImGui.InputText(id, Pointer, (nint)(sz > 0 ? sz : Size), flags, Callback);
        }

        public void InputTextMultiline(string id, Vector2 size, ImGuiInputTextFlags flags, int sz = -1)
        {
            ImGui.InputTextMultiline(id, Pointer, (nint)(sz > 0 ? sz : Size), size, flags, Callback);
        }

		public string GetText()
		{
            return UnsafeHelpers.PtrToStringUTF8(Pointer, Size);
		}

		public void Dispose()
		{
			Marshal.FreeHGlobal(Pointer);
		}
	}
}
