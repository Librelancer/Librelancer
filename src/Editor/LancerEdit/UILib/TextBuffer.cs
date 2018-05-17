using System;
using System.Text;
using System.Runtime.InteropServices;
using ImGuiNET;
namespace LancerEdit
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
			int len = Size;
			for (int i = 0; i < Size; i++)
			{
				var ptr = (byte*)Pointer;
				if (ptr[i] == 0)
				{
					len = i;
					break;
				}
			}
			var bytes = new byte[len];
			Marshal.Copy(Pointer, bytes, 0, len);
			return Encoding.UTF8.GetString(bytes);
		}

		public void Dispose()
		{
			Marshal.FreeHGlobal(Pointer);
		}
	}
}
