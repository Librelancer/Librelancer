// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

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

        [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void alcGetIntegerv(IntPtr device, int param, IntPtr size, ref int values);

        [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr alcGetProcAddress(IntPtr dev, [MarshalAs(UnmanagedType.LPUTF8Str)]string proc);

        public const int ALC_CONNECTED = 0x313;
	}
}

