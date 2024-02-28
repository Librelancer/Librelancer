using System;
using System.IO;

namespace LibreLancer.ContentEdit;

// Stream class that just increments a counter
class WriteStatisticsStream : Stream
{
    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        _position += buffer.Length;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        _position += count;
    }

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

    public void Reset()
    {
        _position = 0;
    }
}
