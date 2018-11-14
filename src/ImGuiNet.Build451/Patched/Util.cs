using System;

namespace ImGuiNET
{
    internal static class Util
    {
        public static unsafe string StringFromPtr(byte* ptr)
        {
            return LibreLancer.UnsafeHelpers.PtrToStringUTF8((IntPtr)ptr);

        }

        internal static unsafe bool AreStringsEqual(byte* a, int aLength, byte* b)
        {
            for (int i = 0; i < aLength; i++)
            {
                if (a[i] != b[i]) { return false; }
            }

            if (b[aLength] != 0) { return false; }

            return true;
        }
    }
}
