using System;
using System.Collections.Generic;

namespace LibreLancer.Utf.Anm;

public class AnmBuffer
{
    public byte[] Buffer = [];

    private List<ArraySegment<byte>> buffers = [];
    private int usedCount = 0;

    public virtual int Append(ArraySegment<byte> segment)
    {
        var ptr = usedCount;
        buffers.Add(segment);
        usedCount += segment.Count;
        return ptr;
    }

    public void Commit()
    {
        int offset = 0;
        Buffer = new byte[usedCount];
        foreach (var b in buffers)
        {
            b.CopyTo(Buffer, offset);
            offset += b.Count;
        }
        usedCount = 0;
        buffers = [];
    }
}
