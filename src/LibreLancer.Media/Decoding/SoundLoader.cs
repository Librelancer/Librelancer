// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Runtime.InteropServices;
namespace LibreLancer.Media
{
	static class SoundLoader
	{
		public static StreamingSound Open(Stream stream)
		{
            var dec = new Decoder(stream);
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
    enum LdFormat
    {
        Mono8 = 1,
        Mono16 = 2,
        Stereo8 = 3,
        Stereo16 = 4
    }

    unsafe class Decoder : Stream
    {
        enum LdSeek
        {
            SET = 1,
            CUR = 2,
            END = 3
        }
        [StructLayout(LayoutKind.Sequential)]
        struct ld_stream
        {
            public IntPtr read;
            public IntPtr seek;
            public IntPtr tell;
            public IntPtr close;
            public IntPtr userData;
        }
        [StructLayout(LayoutKind.Sequential)]
        unsafe struct ld_pcmstream
        {
            public ld_stream* stream;
            public int dataSize;
            public int frequency;
            public LdFormat format;
            public int blockSize;
        }
        delegate void ErrorCallback(IntPtr t);
        static ErrorCallback err;
        [DllImport("lancerdecode", CallingConvention = CallingConvention.Cdecl)]
        static extern void ld_errorlog_register(ErrorCallback cb);
        static void OnError(IntPtr t)
        {
            FLLog.Error("LancerDecode", Marshal.PtrToStringAnsi(t));
        }
        static Decoder()
        {
            err = OnError;
            ld_errorlog_register(err);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate IntPtr ReadFn(byte* buffer, IntPtr size, ld_stream* stream);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int SeekFn(ld_stream* stream, int offset, LdSeek seek);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int TellFn(ld_stream* stream);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void CloseFn(ld_stream* stream);

        [DllImport("lancerdecode", CallingConvention = CallingConvention.Cdecl)]
        static extern ld_stream* ld_stream_new();
        [DllImport("lancerdecode", CallingConvention = CallingConvention.Cdecl)]
        static extern void ld_stream_destroy(ld_stream* stream);
        [DllImport("lancerdecode", CallingConvention = CallingConvention.Cdecl)]
        static extern void ld_pcmstream_close(ld_pcmstream* stream);
        [DllImport("lancerdecode", CallingConvention = CallingConvention.Cdecl)]
        static extern ld_pcmstream* ld_pcmstream_open(ld_stream* stream);

        ld_stream* self;
        ld_pcmstream* decoder;

        ReadFn selfRead;
        SeekFn selfSeek;
        TellFn selfTell;
        CloseFn selfClose;

        ReadFn decoderRead;
        SeekFn decoderSeek;
        Stream baseStream;

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

        private IntPtr csbuffer = IntPtr.Zero;
        private int csbufferLength = -1;

        IntPtr StreamRead(byte* buffer, IntPtr size, ld_stream* stream)
        {
            var sz = (int)size;

            if (csbufferLength < sz)
            {
                if (csbuffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(csbuffer);
                }
                csbuffer = Marshal.AllocHGlobal(sz);
                csbufferLength = sz;
            }
            var buf = new Span<byte>((void*) csbuffer, sz);
            var result = baseStream.Read(buf);
            buf.Slice(0, result).CopyTo(new Span<byte>((void*)buffer, result));
            return result;
        }

        int StreamSeek(ld_stream* stream, int offset, LdSeek seek)
        {
            var origin = SeekOrigin.Begin;
            if (seek == LdSeek.END) origin = SeekOrigin.End;
            if (seek == LdSeek.CUR) origin = SeekOrigin.Current;
            var end = (int)baseStream.Seek(offset, origin);
            return end;
        }

        int StreamTell(ld_stream* stream)
        {
            return (int)baseStream.Position;
        }

        void StreamClose(ld_stream* stream)
        {
            baseStream.Close();
            ld_stream_destroy(stream);
        }

        public Decoder(Stream stream)
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
            decoder = ld_pcmstream_open(self);
            if ((IntPtr)decoder == IntPtr.Zero)
            {
                throw new Exception("ld_pcmstream_open failed");
            }

            decoderRead = (ReadFn)Marshal.GetDelegateForFunctionPointer(decoder->stream->read, typeof(ReadFn));
            decoderSeek = (SeekFn)Marshal.GetDelegateForFunctionPointer(decoder->stream->seek, typeof(SeekFn));
        }

        void Reset()
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
                if (csbuffer != IntPtr.Zero) {
                    Marshal.FreeHGlobal(csbuffer);
                    csbuffer = IntPtr.Zero;
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }
    }
}

