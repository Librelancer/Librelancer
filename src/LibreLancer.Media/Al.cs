// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.InteropServices;
namespace LibreLancer.Media
{
	static class Al
	{
		const string lib = "openal32.dll";

		//CONSTANTS
		public const int AL_PITCH = 0x1003;
		public const int AL_GAIN = 0x100A;
		public const int AL_BUFFER = 0x1009;
        public const int AL_POSITION = 0x1004;
        public const int AL_VELOCITY = 0x1006;
        public const int AL_LOOPING = 0x1007;

        public const int AL_REFERENCE_DISTANCE = 0x1020;
        public const int AL_MAX_DISTANCE = 0x1023;

        public const int AL_FORMAT_MONO8 = 0x1100;
		public const int AL_FORMAT_MONO16 = 0x1101;
		public const int AL_FORMAT_STEREO8 = 0x1102;
		public const int AL_FORMAT_STEREO16 = 0x1103;

		public const int AL_BUFFERS_QUEUED = 0x1015;
		public const int AL_BUFFERS_PROCESSED = 0x1016;

		public const int AL_SOURCE_STATE = 0x1010;
        public const int AL_SOURCE_RELATIVE = 0x202;
		public const int AL_PLAYING = 0x1012;
		public const int AL_PAUSED = 0x1013;
		public const int AL_STOPPED = 0x1014;

		public const int AL_NO_ERROR = 0;
        //FUNCTIONS
        [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void alGenSources(int n, out uint sources);

		public static uint GenSource()
		{
			uint s;
			alGenSources(1, out s);
			return s;
		}

        [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void alGenBuffers(int n, out uint buffers);

        [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void alGenBuffers(int n, uint[] buffers);

		public static uint GenBuffer()
		{
			uint b;
			alGenBuffers(1, out b);
			return b;
		}

        [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void alListener3f(int param, float value1, float value2, float value3);

        [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void alSourcef(uint sid, int param, float value);

        [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void alSource3f(uint sid, int param, float value1, float value2, float value3);

        [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void alSourcei(uint sid, int param, int value);

        [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void alGetSourcei(uint sid, int param, out int value);

        [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void alBufferData(uint bid, int format, IntPtr buffer, int size, int freq);

		public static unsafe void BufferData(uint bid, int format, byte[] buffer, int size, int freq)
		{
			fixed(byte* ptr = buffer)
			{
				alBufferData(bid, format, (IntPtr)ptr, size, freq);
			}
		}

        [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void alSourcePlay(uint sid);

        [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void alSourceStopv(int n, ref uint sids);

        [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void alSourcePausev(int n, ref uint sids);

        [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void alSourceUnqueueBuffers(uint sid, int n, ref uint bids);

        [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void alSourceUnqueueBuffers(uint sid, int n, uint[] bids);

        [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void alSourceQueueBuffers(uint sid, int r, ref uint bids);

        [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern int alGetError();

        [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr alGetString(int param);

        [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void alDeleteBuffers(int i, ref uint buffers);
		public static string GetString(int param)
		{
			return Marshal.PtrToStringAnsi(alGetString(param));
		}

		public static void CheckErrors()
		{
			int error;
			if ((error = Al.alGetError()) != Al.AL_NO_ERROR)
				throw new InvalidOperationException(Al.GetString(error));
		}
	}
}

