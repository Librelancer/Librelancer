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

        class Native
        {
            [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
            public static extern void alGenSources(int n, out uint sources);

            [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
            public static extern void alGenBuffers(int n, out uint buffers);

            [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
            public static extern void alGenBuffers(int n, uint[] buffers);


            [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
            public static extern void alListenerfv(int param, IntPtr values);

            [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
            public static extern void alListenerf(int param, float value);

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


            [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
            public static extern void alSourcePlay(uint sid);

            [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
            public static extern void alSourceStopv(int n, ref uint sids);

            [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
            public static extern void alSourcePausev(int n, ref uint sids);

            [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
            public static extern void alSourceUnqueueBuffers(uint sid, int n, ref uint bids);
            
            [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
            public static extern void alSourceQueueBuffers(uint sid, int r, ref uint bids);

            [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
            public static extern int alGetError();

            [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr alGetString(int param);

            [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
            public static extern void alDeleteBuffers(int i, ref uint buffers);

            [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
            public static extern void alDopplerFactor(float factor);

            [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
            public static extern void alDisable(int name);

        }

        //CONSTANTS
        public const int AL_CONE_INNER_ANGLE = 0x1001;
        public const int AL_CONE_OUTER_ANGLE = 0x1002;
        public const int AL_PITCH = 0x1003;
        public const int AL_GAIN = 0x100A;
        public const int AL_BUFFER = 0x1009;
        public const int AL_POSITION = 0x1004;
        public const int AL_DIRECTION = 0x1005;
        public const int AL_VELOCITY = 0x1006;
        public const int AL_LOOPING = 0x1007;
        public const int AL_ORIENTATION = 0x100F;
        public const int AL_REFERENCE_DISTANCE = 0x1020;
        public const int AL_CONE_OUTER_GAIN = 0x1022;
        public const int AL_MAX_DISTANCE = 0x1023;
        public const int AL_SEC_OFFSET = 0x1024;

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

        //OpenAL SOFT extensions
        public const int AL_STOP_SOURCES_ON_DISCONNECT_SOFT = 0x19AB;

        //FUNCTIONS

        public static void alListenerf(int param, float value)
        {
            Native.alListenerf(param, value);
            CheckErrors();
        }

        public static void alDisable(int param)
        {
            Native.alDisable(param);
            CheckErrors();
        }

        public static void alSourcef(uint sid, int param, float value)
        {
            Native.alSourcef(sid, param, value);
            CheckErrors();
        }

        public static void alSource3f(uint sid, int param, float value1, float value2, float value3)
        {
            Native.alSource3f(sid, param, value1, value2, value3);
            CheckErrors();
        }

        public static void alSourcei(uint sid, int param, int value)
        {
            Native.alSourcei(sid, param, value);
            CheckErrors();
        }

        public static void alSourcePlay(uint sid)
        {
            Native.alSourcePlay(sid);
            CheckErrors();
        }

        public static uint GenSource()
		{
			uint s;
			Native.alGenSources(1, out s);
            CheckErrors();
			return s;
		}

      
		public static uint GenBuffer()
		{
			uint b;
			Native.alGenBuffers(1, out b);
            CheckErrors();
			return b;
		}

        public static unsafe void alListener3f(int param, float value1, float value2, float value3)
        {
            float* floats = stackalloc float[3];
            floats[0] = value1;
            floats[1] = value2;
            floats[2] = value3;
            Native.alListenerfv(param, (IntPtr)floats);
            CheckErrors();
        }
        
        public static void alListenerfv(int param, IntPtr value)
        {
            Native.alListenerfv(param, value);
            CheckErrors();
        }

       

		public static unsafe void BufferData(uint bid, int format, byte[] buffer, int size, int freq)
		{
			fixed(byte* ptr = buffer)
			{
				Native.alBufferData(bid, format, (IntPtr)ptr, size, freq);
			}
            int error;
            if ((error = Native.alGetError()) != Al.AL_NO_ERROR)
            {
                var str = $"alBufferData({bid}, {format}, void*, {size}, {freq}) - {GetString(error)}";
                throw new InvalidOperationException(str);
            }
        }

        public static void alGetSourcei(uint sid, int param, out int value)
        {
            Native.alGetSourcei(sid, param, out value);
            CheckErrors();
        }

        public static void alSourceStopv(int n, ref uint sids)
        {
            Native.alSourceStopv(n, ref sids);
            CheckErrors();
        }

        public static void alSourcePausev(int n, ref uint sids)
        {
            Native.alSourceStopv(n, ref sids);
            CheckErrors();
        }

        public static void alSourceUnqueueBuffers(uint sid, int n, ref uint bids)
        {
            Native.alSourceUnqueueBuffers(sid, n, ref bids);
            CheckErrors();
        }

        public static void alSourceQueueBuffers(uint sid, int r, ref uint bids)
        {
            Native.alSourceQueueBuffers(sid, r, ref bids);
            CheckErrors();
        }

        public static void alDeleteBuffers(int i, ref uint buffers)
        {
            Native.alDeleteBuffers(i, ref buffers);
            CheckErrors();
        }

        public static void alDopplerFactor(float factor)
        {
            Native.alDopplerFactor(factor);
            CheckErrors();
        }

        public static string GetString(int param)
		{
			return Marshal.PtrToStringAnsi(Native.alGetString(param));
		}

        
        [System.Diagnostics.DebuggerHidden]
        static void CheckErrors()
		{
			int error;
			if ((error = Native.alGetError()) != Al.AL_NO_ERROR)
				throw new InvalidOperationException(Al.GetString(error));
		}
	}
}

