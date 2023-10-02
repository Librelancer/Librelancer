using System;

namespace LibreLancer.Utf.Anm;

public class AnmBuffer
{
    public byte[] Buffer = new byte[2048];
    private int usedCount = 0;

    public int Take(int size)
    {
        if (Buffer.Length < usedCount + size)
        {
            int newSize = Math.Max(Buffer.Length * 2, usedCount + size);
            Array.Resize(ref Buffer, newSize);
        }

        var t = usedCount;
        usedCount += size;
        return t;
    }

    public void Shrink()
    {
        if (Buffer.Length > usedCount) {
            Array.Resize(ref Buffer, usedCount);
        }
    }
}
