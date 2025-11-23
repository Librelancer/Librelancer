// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Text;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ImGuiNET;
using LibreLancer;
namespace LibreLancer.ImUI
{
	public unsafe class TextBuffer : IDisposable
	{
		public int Size;
		public NativeBuffer NativeMemory;
		public ImGuiInputTextCallback Callback;
        public TextBuffer(int sz = 2048)
        {
            sz = (sz + 7) & ~7;
            Size = sz;
            NativeMemory = UnsafeHelpers.Allocate(sz);
			Clear();
			Callback = HandleTextEditCallback;
		}

		public void Clear()
		{
            Unsafe.InitBlockUnaligned((void*)NativeMemory.Handle, 0, (uint)NativeMemory.Size);
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
            var dest = new Span<byte>((void*)NativeMemory.Handle, bytes.Length + 1);
            bytes.CopyTo(dest);
            dest[^1] = 0;
		}

        public unsafe void InputText(string id, ImGuiInputTextFlags flags, int sz = -1)
        {
            ImGui.InputText(id, NativeMemory.Handle, (nint)(sz > 0 ? sz : Size), flags, Callback);
        }

        public void InputTextMultiline(string id, Vector2 size, ImGuiInputTextFlags flags, int sz = -1)
        {
            ImGui.InputTextMultiline(id, NativeMemory.Handle, (nint)(sz > 0 ? sz : Size), size, flags, Callback);
        }

		public string GetText()
		{
            return UnsafeHelpers.PtrToStringUTF8(NativeMemory.Handle, Size);
		}

		public void Dispose()
        {
            NativeMemory.Dispose();
        }
	}
}
