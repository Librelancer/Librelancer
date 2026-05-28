namespace LibreLancer.Net.Protocol;

public class NetRleReader
{
    private byte[] buffer;
    private int nibbleIndex;

    public NetRleReader(byte[] buffer, int offset = 0)
    {
        this.buffer = buffer;
        nibbleIndex = offset * 2;
    }

    byte ReadNibble()
    {
        var byteIndex = nibbleIndex >> 1;
        byte v = 0;
        if ((nibbleIndex & 1) == 0)
            v = (byte)((buffer[byteIndex] >> 4) & 0xF);
        else
            v = (byte)(buffer[byteIndex] & 0xF);
        nibbleIndex++;
        return v;
    }

    private int literalCount;
    private int zeroCount;

    public byte ReadByte()
    {
        if (zeroCount > 0)
        {
            zeroCount--;
            return 0;
        }
        if (literalCount > 0)
        {
            literalCount--;
            return (byte)(ReadNibble() << 4 | ReadNibble());
        }

        var b0 = ReadNibble();
        if ((b0 & 0x8) == 0)
        {
            //zero counter
            zeroCount = b0 == 7
                ? 8 + ReadNibble()
                : (b0 + 1);
            // return a zero
            zeroCount--;
            return 0;
        }
        else if ((b0 & 0x4) == 0)
        {
            //small literal
            var b = (b0 & 0x3) << 4;
            return (byte)((b | ReadNibble()) + 1);
        }
        else
        {
            //literal counter, don't + 1 because we read one already
            literalCount = b0 & 0x3;
            return (byte)(ReadNibble() << 4 | ReadNibble());
        }
    }

    public void Read0(ref uint u)
    {
        u |= (uint)ReadByte() << 24;
    }
    public void Read1(ref uint u)
    {
        u |= (uint)ReadByte() << 16;
    }
    public void Read2(ref uint u)
    {
        u |= (uint)ReadByte() << 8;
    }
    public void Read3(ref uint u)
    {
        u |= (uint)ReadByte();
    }
}
