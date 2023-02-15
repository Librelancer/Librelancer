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
			var bytes = Encoding.UTF8.GetBytes(text);
			Marshal.Copy(bytes, 0, Pointer, bytes.Length);
			Marshal.WriteByte(Pointer, bytes.Length, 0);
		}

		public void SetBytes(byte[] b, int len, bool writeZero = true)
		{
			Marshal.Copy(b, 0, Pointer, len);
			if (writeZero)
				Marshal.WriteByte(Pointer,len,0);
		}

        public unsafe void InputText(string id, ImGuiInputTextFlags flags, int sz = -1)
        {
            var idBytes = UnsafeHelpers.StringToHGlobalUTF8(id);
            ImGuiNative.igInputText((byte*)idBytes, (byte*)Pointer, (uint)(sz > 0 ? sz : Size), flags, Callback, (void*)0);
            Marshal.FreeHGlobal(idBytes);
        }

        public void InputTextMultiline(string id, Vector2 size, ImGuiInputTextFlags flags, int sz = -1)
        {
            var idBytes = UnsafeHelpers.StringToHGlobalUTF8(id);
            ImGuiNative.igInputTextMultiline((byte*)idBytes, (byte*)Pointer, (uint)(sz > 0 ? sz : Size), size, flags, Callback, (void*)0);
            Marshal.FreeHGlobal(idBytes);
        }

        public byte[] GetByteArray()
		{
			int len = Size;
			for (int i = 0; i < Size; i++)
			{
				var ptr = (byte*)Pointer;
				if (ptr[i] == 0)
				{
					len = i + 1;
					break;
				}
			}
			var bytes = new byte[len];
			Marshal.Copy(Pointer, bytes, 0, len);
			return bytes;
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
