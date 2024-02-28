using System;
using System.IO;
using System.Runtime.InteropServices;

namespace LibreLancer.ContentEdit;

// Use unmanaged memory to reduce GC pressure
class UnmanagedWriteStream : Stream
{
    private IntPtr unmanagedBuffer;
    private long bufferSize;

    private const int DefaultSize = 512 * 1024;

    public UnmanagedWriteStream()
    {
        unmanagedBuffer = Marshal.AllocHGlobal(DefaultSize);
        bufferSize = DefaultSize;
    }

    private const int WriteSize = 32 * 1024 * 1024;
    public unsafe void WriteAndDispose(Stream output)
    {
        long sz = Position;
        long offset = 0;
        while (sz > WriteSize)
        {
            output.Write(new Span<byte>((void*)(unmanagedBuffer + offset), WriteSize));
            offset += WriteSize;
            sz -= WriteSize;
        }
        if (sz > 0)
        {
            output.Write(new Span<byte>((void*)(unmanagedBuffer + offset), (int)sz));
        }
        Marshal.FreeHGlobal(unmanagedBuffer);
        unmanagedBuffer = IntPtr.Zero;
    }

    protected override void Dispose(bool disposing)
    {
        if (unmanagedBuffer != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(unmanagedBuffer);
            unmanagedBuffer = IntPtr.Zero;
        }
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override unsafe void Write(ReadOnlySpan<byte> buffer)
    {
        if (bufferSize < _position + buffer.Length)
        {
            var newSize = Math.Max(bufferSize * 2, _position + buffer.Length + 1);
            bufferSize = newSize;
            unmanagedBuffer = Marshal.ReAllocHGlobal(unmanagedBuffer, (IntPtr)newSize);
        }

        fixed (byte* src = &buffer.GetPinnableReference())
            Buffer.MemoryCopy((void*)src, (void*)(unmanagedBuffer + _position), (bufferSize - _position), buffer.Length);
        _position += buffer.Length;
    }

    public override void Write(byte[] buffer, int offset, int count) =>
        Write(buffer.AsSpan().Slice(offset, count));

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => _position;

    private long _position = 0;

    public override long Position
    {
        get => _position;
        set => throw new NotSupportedException();
    }
}
