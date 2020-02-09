using System;
namespace LibreLancer
{
    internal unsafe class FNV1A
    {
        public static int Hash(IntPtr input, int sz)
        {
            var bytes = (byte*) input;
            unchecked
            {
                uint hash = 2166136261;
                for (int i = 0; i < sz; i++)
                    hash = (hash ^ bytes[i]) * 16777619;
                return (int) hash;
            }
        }
    }
}