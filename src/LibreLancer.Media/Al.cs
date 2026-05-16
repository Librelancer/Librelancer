// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.InteropServices;
namespace LibreLancer.Media;

internal static unsafe class Al
{
    public static class Native
    {
        public static delegate* unmanaged<int, uint*, void> alGenSources;
        public static delegate* unmanaged<int, uint*, void> alGenBuffers;
        public static delegate* unmanaged<int, float*, void> alListenerfv;
        public static delegate* unmanaged<int, float, void> alListenerf;
        public static delegate* unmanaged<uint, int, float, void> alSourcef;
        public static delegate* unmanaged<uint, int, float, float, float, void> alSource3f;
        public static delegate* unmanaged<uint, int, int, void> alSourcei;
        public static delegate* unmanaged<uint, int, int*, void> alGetSourcei;
        public static delegate* unmanaged<uint, int, IntPtr, int, int, void> alBufferData;
        public static delegate* unmanaged<uint, void> alSourcePlay;
        public static delegate* unmanaged<int, uint*, void> alSourceStopv;
        public static delegate* unmanaged<int, uint*, void> alSourcePausev;
        public static delegate* unmanaged<uint, int, uint*, void> alSourceUnqueueBuffers;
        public static delegate* unmanaged<uint, int, uint*, void> alSourceQueueBuffers;
        public static delegate* unmanaged<int> alGetError;
        public static delegate* unmanaged<int, IntPtr> alGetString;
        public static delegate* unmanaged<int, uint*, void> alDeleteBuffers;
        public static delegate* unmanaged<float, void> alDopplerFactor;
        public static delegate* unmanaged<int, void> alDisable;
        public static delegate* unmanaged<IntPtr, IntPtr> alGetProcAddress;

        public static void LoadFunctions(IntPtr library)
        {
            alGenSources = (delegate* unmanaged<int, uint*, void>)NativeLibrary.GetExport(library, "alGenSources");
            alGenBuffers = (delegate* unmanaged<int, uint*, void>)NativeLibrary.GetExport(library, "alGenBuffers");
            alListenerfv = (delegate* unmanaged<int, float*, void>)NativeLibrary.GetExport(library, "alListenerfv");
            alListenerf = (delegate* unmanaged<int, float, void>)NativeLibrary.GetExport(library, "alListenerf");
            alSourcef = (delegate* unmanaged<uint, int, float, void>)NativeLibrary.GetExport(library, "alSourcef");
            alSource3f = (delegate* unmanaged<uint, int, float, float, float, void>)NativeLibrary.GetExport(library, "alSource3f");
            alSourcei = (delegate* unmanaged<uint, int, int, void>)NativeLibrary.GetExport(library, "alSourcei");
            alGetSourcei = (delegate* unmanaged<uint, int, int*, void>)NativeLibrary.GetExport(library, "alGetSourcei");
            alBufferData = (delegate* unmanaged<uint, int, IntPtr, int, int, void>)NativeLibrary.GetExport(library, "alBufferData");
            alSourcePlay = (delegate* unmanaged<uint, void>)NativeLibrary.GetExport(library, "alSourcePlay");
            alSourceStopv = (delegate* unmanaged<int, uint*, void>)NativeLibrary.GetExport(library, "alSourceStopv");
            alSourcePausev = (delegate* unmanaged<int, uint*, void>)NativeLibrary.GetExport(library, "alSourcePausev");
            alSourceUnqueueBuffers = (delegate* unmanaged<uint, int, uint*, void>)NativeLibrary.GetExport(library, "alSourceUnqueueBuffers");
            alSourceQueueBuffers = (delegate* unmanaged<uint, int, uint*, void>)NativeLibrary.GetExport(library, "alSourceQueueBuffers");
            alGetError = (delegate* unmanaged<int>)NativeLibrary.GetExport(library, "alGetError");
            alGetString = (delegate* unmanaged<int, IntPtr>)NativeLibrary.GetExport(library, "alGetString");
            alDeleteBuffers = (delegate* unmanaged<int, uint*, void>)NativeLibrary.GetExport(library, "alDeleteBuffers");
            alDopplerFactor = (delegate* unmanaged <float, void>)NativeLibrary.GetExport(library, "alDopplerFactor");
            alDisable = (delegate* unmanaged<int, void>)NativeLibrary.GetExport(library, "alDisable");
            alGetProcAddress = (delegate* unmanaged<IntPtr, IntPtr>)NativeLibrary.GetExport(library, "alGetProcAddress");
        }
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

    public static IntPtr alGetProcAddress(string procName)
    {
        using var n = UnsafeHelpers.StringToNativeUTF8(procName);
        return Native.alGetProcAddress(n.Handle);
    }

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
        Native.alGenSources(1, &s);
        CheckErrors();
        return s;
    }


    public static uint GenBuffer()
    {
        uint b;
        Native.alGenBuffers(1, &b);
        CheckErrors();
        return b;
    }

    public static unsafe void alListener3f(int param, float value1, float value2, float value3)
    {
        float* floats = stackalloc float[3];
        floats[0] = value1;
        floats[1] = value2;
        floats[2] = value3;
        Native.alListenerfv(param, floats);
        CheckErrors();
    }

    public static void alListenerfv(int param, IntPtr value)
    {
        Native.alListenerfv(param, (float*)value);
        CheckErrors();
    }


    public static unsafe void BufferData(uint bid, int format, IntPtr buffer, int size, int freq)
    {
        Native.alBufferData(bid, format, buffer, size, freq);
        int error;
        if ((error = Native.alGetError()) != Al.AL_NO_ERROR)
        {
            var str = $"alBufferData({bid}, {format}, void*, {size}, {freq}) - {GetString(error)}";
            throw new InvalidOperationException(str);
        }
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
        int v;
        Native.alGetSourcei(sid, param, &v);
        value = v;
        CheckErrors();
    }

    public static void alSourceStopv(int n, ref uint sids)
    {
        fixed(uint* s =  &sids)
            Native.alSourceStopv(n, s);
        CheckErrors();
    }

    public static void alSourcePausev(int n, ref uint sids)
    {
        fixed(uint* s =  &sids)
            Native.alSourceStopv(n, s);
        CheckErrors();
    }

    public static void alSourceUnqueueBuffers(uint sid, int n, ref uint bids)
    {
        fixed(uint* b =  &bids)
            Native.alSourceUnqueueBuffers(sid, n, b);
        CheckErrors();
    }

    public static void alSourceQueueBuffers(uint sid, int r, ref uint bids)
    {
        fixed(uint* b = &bids)
            Native.alSourceQueueBuffers(sid, r, b);
        CheckErrors();
    }

    public static void alDeleteBuffers(int i, ref uint buffers)
    {
        fixed(uint* b = &buffers)
            Native.alDeleteBuffers(i, b);
        CheckErrors();
    }

    public static void alDopplerFactor(float factor)
    {
        Native.alDopplerFactor(factor);
        CheckErrors();
    }

    public static string GetString(int param)
    {
        return Marshal.PtrToStringAnsi(Native.alGetString(param))!;
    }


    [System.Diagnostics.DebuggerHidden]
    private static void CheckErrors()
    {
        int error;
        if ((error = Native.alGetError()) != Al.AL_NO_ERROR)
            throw new Exception($"AL ERROR {error}: {GetString(error)}");
    }
}
