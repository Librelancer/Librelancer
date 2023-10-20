// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LibreLancer
{
    public struct BitArray128
    {
        public static int Capacity = 128;
        long a;
        long b;

        public BitArray128(ReadOnlySpan<byte> bytes)
        {
            a = BitConverter.ToInt64(bytes.Slice(0, 8));
            b = BitConverter.ToInt64(bytes.Slice(8, 8));
        }

        public void CopyTo(Span<byte> bytes)
        {
            var longs = MemoryMarshal.Cast<byte, long>(bytes);
            longs[0] = a;
            longs[1] = b;
        }

        public bool this[int idx]
        {
            get
            {
                if (idx > 127 || idx < 0)
                    throw new IndexOutOfRangeException();
                if (idx > 63)
                    return (b & (1L << idx - 63)) != 0;
                else
                    return (a & (1L << idx)) != 0;
            }
            set
            {
                if (idx > 127 || idx < 0)
                    throw new IndexOutOfRangeException();
                if (idx > 63)
                {
                    if (value)
                        b |= (1L << (idx - 63));
                    else
                        b &= ~(1L << (idx - 63));
                }
                else
                {
                    if (value)
                        a |= (1L << idx);
                    else
                        a &= ~(1L << idx);
                }
            }
        }

        public bool Any() => a != 0 || b != 0;

        public bool All() => a == -1 && b == -1;

        public static bool operator ==(BitArray128 a, BitArray128 b)
        {
            return a.a == b.a && a.b == b.b;
        }

        public static bool operator !=(BitArray128 a, BitArray128 b)
        {
            return a.a != b.a || a.b != b.b;
        }
        public bool Equals(BitArray128 other)
        {
            return a == other.a && b == other.b;
        }

        public override bool Equals(object obj)
        {
            return obj is BitArray128 other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(a, b);
        }
    }
}
