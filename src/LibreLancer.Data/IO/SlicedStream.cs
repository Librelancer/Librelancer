using System;
using System.IO;

namespace LibreLancer.Data.IO;

internal class SlicedStream : Stream
{
    public long SliceLength;
    public long SliceStart;
    public Stream BaseStream;
    public bool CloseBaseStream = true;

    public SlicedStream(long sliceStart, long sliceLength, Stream baseStream)
    {
        SliceStart = sliceStart;
        SliceLength = sliceLength;
        BaseStream = baseStream;
        baseStream.Seek(sliceStart, SeekOrigin.Begin);
    }

    protected override void Dispose(bool disposing)
    {
        if(disposing && CloseBaseStream)
            BaseStream.Dispose();
    }

    public override void Flush() { }

    public override int Read(Span<byte> buffer)
    {
        var remaining = SliceLength - Position;
        if (remaining < buffer.Length)
            return BaseStream.Read(buffer.Slice(0, (int)remaining));
        return BaseStream.Read(buffer);
    }

    public override int Read(byte[] buffer, int offset, int count) =>
        Read(buffer.AsSpan().Slice(offset, count));

    public override long Seek(long offset, SeekOrigin origin)
    {
        switch (origin)
        {
            case SeekOrigin.Begin:
                if (offset < 0 || offset > SliceLength)
                    throw new IOException();
                BaseStream.Seek(SliceStart + offset, SeekOrigin.Begin);
                return Position;
            case SeekOrigin.End:
                if (offset > 0 || offset < -SliceLength)
                    throw new IOException();
                BaseStream.Seek(SliceStart + SliceLength + offset, SeekOrigin.Begin);
                return Position;
            case SeekOrigin.Current:
                var p = BaseStream.Position + offset;
                if (p < SliceStart || p > SliceStart + SliceLength)
                    throw new IOException();
                BaseStream.Seek(offset, SeekOrigin.Current);
                return Position;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => SliceLength;

    public override long Position
    {
        get => BaseStream.Position - SliceStart;
        set => Seek(value, SeekOrigin.Begin);
    }
}
