using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LibreLancer;

[StructLayout(LayoutKind.Sequential)]
public struct BitArray512
{
    public static int Capacity = 512;
    private long A;
    private long B;
    private long C;
    private long D;
    private long E;
    private long F;
    private long G;
    private long H;

    static ref long Element(int index, ref BitArray512 x)
    {
        if (index < 0 || index > 511) throw new IndexOutOfRangeException();
        return ref Unsafe.Add(ref x.A, (nuint) (index >> 6));
    }

    public bool this[int index]
    {
        get
        {
            return (Element(index, ref this) & (1L << (index & 0x3F))) != 0;
        }
        set
        {
            if (value)
                Element(index, ref this) |= (1L << (index & 0x3F));
            else
                Element(index, ref this) &= ~(1L << (index & 0x3F));
        }
    }

    public bool Any() => A != 0 || B != 0 || C != 0 || D != 0 || E != 0 || F != 0 || G != 0 || H != 0;
}
