using System;
using System.Text;
using LibreLancer;

namespace ImGuiNET
{
    public static unsafe class Polyfill
    {
        public static string GetString(this Encoding encoding, byte *ptr, int length)
        {
            if (encoding == Encoding.UTF8)
                return UnsafeHelpers.PtrToStringUTF8((IntPtr) ptr, length);
            throw new Exception("Invalid encoding GetString(byte*,int)");
        }
    }
}