// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Runtime.InteropServices;
using ZstdSharp;

namespace LibreLancer.Media;

internal static class SoundLoader
{
    public static StreamingSound Open(Stream stream)
    {
        var dec = new AudioDecoder(stream);
        var sound = new StreamingSound();
        sound.Data = dec;
        switch(dec.Format)
        {
            case LdFormat.Mono8:
                sound.Format = Al.AL_FORMAT_MONO8;
                break;
            case LdFormat.Mono16:
                sound.Format = Al.AL_FORMAT_MONO16;
                break;
            case LdFormat.Stereo8:
                sound.Format = Al.AL_FORMAT_STEREO8;
                break;
            case LdFormat.Stereo16:
                sound.Format = Al.AL_FORMAT_STEREO16;
                break;
        }
        sound.Frequency = dec.Frequency;
        return sound;
    }
}
public enum LdFormat
{
    Mono8 = 1,
    Mono16 = 2,
    Stereo8 = 3,
    Stereo16 = 4
}

public record struct AudioProperty(string Name)
{
    public static readonly AudioProperty Codec = new("ld.codec");
    public static readonly AudioProperty Container = new("ld.container");
    public static readonly AudioProperty FlTrim = new ("fl.trim");
    public static readonly AudioProperty FlSamples = new ("fl.samples");
    public static readonly AudioProperty Mp3Trim = new ("mp3.trim");
    public static readonly AudioProperty Mp3Samples = new ("mp3.samples");
}

public unsafe class AudioDecoder : Stream
{
    private enum LdSeek
    {
        SET = 1,
        CUR = 2,
        END = 3
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct ld_stream
    {
        public IntPtr read;
        public IntPtr seek;
        public IntPtr tell;
        public IntPtr close;
        public IntPtr userData;
    }
    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct ld_pcmstream
    {
        public ld_stream* stream;
        public int dataSize;
        public int frequency;
        public LdFormat format;
        public int blockSize;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr ReadFn(byte* buffer, IntPtr size, ld_stream* stream);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int SeekFn(ld_stream* stream, int offset, LdSeek seek);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int TellFn(ld_stream* stream);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void CloseFn(ld_stream* stream);

    [DllImport("lancerdecode", CallingConvention = CallingConvention.Cdecl)]
    private static extern ld_stream* ld_stream_new();
    [DllImport("lancerdecode", CallingConvention = CallingConvention.Cdecl)]
    private static extern void ld_stream_destroy(ld_stream* stream);
    [DllImport("lancerdecode", CallingConvention = CallingConvention.Cdecl)]
    private static extern void ld_pcmstream_close(ld_pcmstream* stream);
    [DllImport("lancerdecode", CallingConvention = CallingConvention.Cdecl)]
    private static extern ld_pcmstream* ld_pcmstream_open(ld_stream* stream, IntPtr options, IntPtr* error);

    [DllImport("lancerdecode", CallingConvention = CallingConvention.Cdecl)]
    private static extern int ld_pcmstream_get_int(ld_pcmstream* stream,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string property, int* value);

    [DllImport("lancerdecode", CallingConvention = CallingConvention.Cdecl)]
    private static extern int ld_pcmstream_get_string(ld_pcmstream* stream,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string property, byte* buffer, int size);


    private ld_stream* self;
    private ld_pcmstream* decoder;

    private ReadFn selfRead;
    private SeekFn selfSeek;
    private TellFn selfTell;
    private CloseFn selfClose;

    private ReadFn decoderRead;
    private SeekFn decoderSeek;
    private Stream baseStream;

    public int Frequency
    {
        get { return decoder->frequency; }
    }
    public LdFormat Format
    {
        get { return decoder->format; }
    }
    public int BlockSize
    {
        get { return decoder->blockSize; }
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotImplementedException();

    public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    private NativeBuffer? csbuffer = null;
    private int csbufferLength = -1;

    private IntPtr StreamRead(byte* buffer, IntPtr size, ld_stream* stream)
    {
        var sz = (int)size;

        if (csbufferLength < sz)
        {
            csbuffer?.Dispose();
            csbuffer = UnsafeHelpers.Allocate(sz);
            csbufferLength = sz;
        }
        var buf = new Span<byte>((void*) csbuffer!, sz);
        var result = baseStream.Read(buf);
        buf.Slice(0, result).CopyTo(new Span<byte>((void*)buffer, result));
        return result;
    }

    private int StreamSeek(ld_stream* stream, int offset, LdSeek seek)
    {
        var origin = SeekOrigin.Begin;
        if (seek == LdSeek.END) origin = SeekOrigin.End;
        if (seek == LdSeek.CUR) origin = SeekOrigin.Current;
        var end = (int)baseStream.Seek(offset, origin);
        return end;
    }

    private int StreamTell(ld_stream* stream)
    {
        return (int)baseStream.Position;
    }

    private void StreamClose(ld_stream* stream)
    {
        baseStream.Close();
        ld_stream_destroy(stream);
    }

    public AudioDecoder(Stream stream)
    {
        baseStream = stream;
        self = ld_stream_new();
        selfRead = StreamRead;
        selfSeek = StreamSeek;
        selfTell = StreamTell;
        selfClose = StreamClose;
        self->read = Marshal.GetFunctionPointerForDelegate(selfRead);
        self->seek = Marshal.GetFunctionPointerForDelegate(selfSeek);
        self->tell = Marshal.GetFunctionPointerForDelegate(selfTell);
        self->close = Marshal.GetFunctionPointerForDelegate(selfClose);
        IntPtr errorPtr = IntPtr.Zero;
        decoder = ld_pcmstream_open(self, IntPtr.Zero, &errorPtr);
        if ((IntPtr)decoder == IntPtr.Zero)
        {
            throw new Exception($"ld_pcmstream_open failed: {Marshal.PtrToStringUTF8(errorPtr)}");
        }

        decoderRead = (ReadFn)Marshal.GetDelegateForFunctionPointer(decoder->stream->read, typeof(ReadFn));
        decoderSeek = (SeekFn)Marshal.GetDelegateForFunctionPointer(decoder->stream->seek, typeof(SeekFn));
    }

    public bool GetInt(AudioProperty property, out int value)
    {
        value = 0;
        fixed(int* v = &value)
            return ld_pcmstream_get_int(decoder, property.Name, v) == 1;
    }

    public bool GetString(AudioProperty property, out string value)
    {
        value = "";
        Span<byte> buffer = stackalloc byte[256];
        fixed (byte* p = buffer)
        {
            if (ld_pcmstream_get_string(decoder, property.Name, p, buffer.Length) > 0)
            {
                value = Marshal.PtrToStringUTF8((IntPtr)p)!;
                return true;
            }
            return false;
        }
    }

    private void Reset()
    {
        decoderSeek(decoder->stream, 0, LdSeek.SET);
    }

    public override void Flush()
    {
        throw new NotImplementedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        fixed(byte* b = buffer)
        {
            return (int)decoderRead(&b[offset], count, decoder->stream);
        }
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        if(origin != SeekOrigin.Begin || offset != 0)
            throw new InvalidOperationException();
        Reset();
        return 0;
    }

    public override void SetLength(long value)
    {
        throw new InvalidOperationException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new InvalidOperationException();
    }

    private bool _disposed = false;
    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            ld_pcmstream_close(decoder);
            csbuffer?.Dispose();
            _disposed = true;
        }
        base.Dispose(disposing);
    }
}
