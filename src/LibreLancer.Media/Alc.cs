// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.InteropServices;
namespace LibreLancer.Media
{
	static class Alc
	{
		const string lib = "soft_oal.dll";

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

        [DllImport(lib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "alcGetProcAddress")]
        static extern IntPtr _alcGetProcAddress(IntPtr dev, [MarshalAs(UnmanagedType.LPUTF8Str)]string proc);

        public static IntPtr alcGetProcAddress(IntPtr dev, string proc)
        {
            var result = _alcGetProcAddress(dev, proc);
            if (result == IntPtr.Zero)
                FLLog.Warning("Alc", $"alcGetProcAddress failed for {proc}");
            return result;
        }

        public const int ALC_CONNECTED = 0x313;
        public const int ALC_EVENT_TYPE_DEFAULT_DEVICE_CHANGED_SOFT = 0x19D6;
    }
}

