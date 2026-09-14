// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.InteropServices;

namespace LibreLancer;

public struct BitArray128
{
    public static int Capacity = 128;
    public ulong A;
    public ulong B;

    public BitArray128(ReadOnlySpan<byte> bytes)
    {
        A = BitConverter.ToUInt64(bytes.Slice(0, 8));
        B = BitConverter.ToUInt64(bytes.Slice(8, 8));
    }

    public void CopyTo(Span<byte> bytes)
    {
        var longs = MemoryMarshal.Cast<byte, ulong>(bytes);
        longs[0] = A;
        longs[1] = B;
    }

    public bool this[int idx]
    {
        get
        {
            if (idx is > 127 or < 0)
                throw new IndexOutOfRangeException();
            if (idx > 63)
                return (B & (1UL << idx - 64)) != 0;
            else
                return (A & (1UL << idx)) != 0;
        }
        set
        {
            if (idx is > 127 or < 0)
                throw new IndexOutOfRangeException();
            if (idx > 63)
            {
                if (value)
                    B |= (1UL << (idx - 64));
                else
                    B &= ~(1UL << (idx - 64));
            }
            else
            {
                if (value)
                    A |= (1UL << idx);
                else
                    A &= ~(1UL << idx);
            }
        }
    }

    public bool Any() => A != 0 || B != 0;

    public bool All() => A == ulong.MaxValue && B == ulong.MaxValue;

    public static bool operator ==(BitArray128 a, BitArray128 b)
    {
        return a.A == b.A && a.B == b.B;
    }

    public static bool operator !=(BitArray128 a, BitArray128 b)
    {
        return a.A != b.A || a.B != b.B;
    }

    public bool Equals(BitArray128 other)
    {
        return A == other.A && B == other.B;
    }

    public override bool Equals(object? obj)
    {
        return obj is BitArray128 other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(A, B);
    }
}
