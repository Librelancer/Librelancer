using System;
using System.Runtime.InteropServices;
namespace LibreLancer.Media
{
	static class Alc
	{
		const string lib = "openal32.dll";

		[DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr alcOpenDevice(string dev);

        [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr alcCreateContext(IntPtr device, IntPtr attrs);

        [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void alcDestroyContext(IntPtr ctx);

        [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void alcMakeContextCurrent(IntPtr ctx);

        [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void alcCloseDevice(IntPtr device);
	}
}

