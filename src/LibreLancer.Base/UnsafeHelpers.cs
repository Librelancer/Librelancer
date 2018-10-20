// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Text;
using System.Runtime.InteropServices;
namespace LibreLancer
{
    public static class UnsafeHelpers
    {
        public static byte[] CastArray<T>(T[] src) where T : struct
        {
            var sz = Marshal.SizeOf(typeof(T));
            byte[] dst = new byte[src.Length * sz];
            Buffer.BlockCopy(src, 0, dst, 0, dst.Length);
            return dst;
        }
        public static unsafe string PtrToStringUTF8(IntPtr intptr, int maxBytes = int.MaxValue)
        {
            int i = 0;
            var ptr = (byte*)intptr;
            while (ptr[i] != 0) {
                i++;
                if (i >= maxBytes) break;
            }
            var bytes = new byte[i];
            Marshal.Copy(intptr, bytes, 0, i);
            return Encoding.UTF8.GetString(bytes);
        }
        public static unsafe IntPtr StringToHGlobalUTF8(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            var ptr = Marshal.AllocHGlobal(bytes.Length + 1);
            Marshal.Copy(bytes, 0, ptr, bytes.Length);
            ((byte*)ptr)[bytes.Length] = 0;
            return ptr;
        }
    }
}
