using System;
using System.Text;

namespace LibreLancer.Graphics;

internal ref struct SpanReader
{
    public int Offset;
    public ReadOnlySpan<byte> Span;

    public SpanReader(ReadOnlySpan<byte> span)
    {
        Offset = 0;
        Span = span;
    }

    public byte ReadByte() => Span[Offset++];

    public bool ReadBoolean() => Span[Offset++] != 0;

    public uint ReadVarUInt32()
    {
        uint a = 0;
        int b = ReadByte();
        a = (uint) (b & 0x7f);
        int extraCount = 0;
        //first extra
        if ((b & 0x80) == 0x80) {
            b = ReadByte();
            a |= (uint) ((b & 0x7f) << 7);
            extraCount++;
        }
        //second extra
        if ((b & 0x80) == 0x80) {
            b = ReadByte();
            a |= (uint) ((b & 0x7f) << 14);
            extraCount++;
        }
        //third extra
        if ((b & 0x80) == 0x80) {
            b = ReadByte();
            a |= (uint) ((b & 0x7f) << 21);
            extraCount++;
        }
        //fourth extra
        if ((b & 0x80) == 0x80) {
            b = ReadByte();
            a |= (uint) ((b & 0xf) << 28);
            extraCount++;
        }
        switch (extraCount) {
            case 1: a += 128; break;
            case 2: a += 16512; break;
            case 3: a += 2113663; break;
        }
        return a;
    }

    public string ReadUTF8()
    {
        var len = ReadVarUInt32();
        var str = Encoding.UTF8.GetString(Span.Slice(Offset, (int)len));
        Offset += (int)len;
        return str;
    }

}
