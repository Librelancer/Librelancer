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
using System.Text;
using System.Runtime.InteropServices;
using ImGuiNET;
using LibreLancer;
namespace LibreLancer.ImUI
{
	public unsafe class TextBuffer : IDisposable
	{
		public int Size;
		public IntPtr Pointer;
		public TextEditCallback Callback;
        public TextBuffer(int sz = 2048)
		{
            if ((sz % 8) != 0) throw new Exception("Must be multiple of 8");
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

		int HandleTextEditCallback(TextEditCallbackData* data)
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
