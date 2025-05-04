using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace LibreLancer.Net.Protocol;

static class BitPrimitives
{
    [DoesNotReturn]
    public static void ThrowArgumentOutOfRangeException() => throw new ArgumentOutOfRangeException();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ReadBit(ReadOnlySpan<byte> source, int bitOffset)
    {
        return ((source[bitOffset >> 3] >> (bitOffset & 7)) & 1) == 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ReadUInt32(ReadOnlySpan<byte> source, int bitOffset, int bitCount)
    {
        if (!ValidateArgs(source.Length * 8, bitOffset, bitCount, 32))
            ThrowArgumentOutOfRangeException();
        ulong value = Unsafe.ReadUnaligned<ulong>(ref Unsafe.AsRef(in source[bitOffset >> 3]));
        if (bitCount + (bitOffset & 7) <= 32)
            return ReadValue32((uint)value, bitOffset & 7, bitCount);
        else
            return (uint)ReadValue64(value, bitOffset & 7, bitCount);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ValidateArgs(int availableBits, int bitOffset, int bitCount, int maxBits)
    {
        // check the amount of bits to read is valid
        if (bitCount < 0 || bitCount > maxBits)
            return false;
        // check the start/end offsets aren't out of bounds
        if (bitOffset < 0 || bitOffset + bitCount > availableBits)
            return false;
        return true;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ReadValue32(uint source, int bitOffset, int bitCount)
    {
        if (bitCount == 0)
            return 0;
        return source << (32 - bitCount - bitOffset) >> (32 - bitCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ReadValue64(ulong source, int bitOffset, int bitCount)
    {
        if (bitCount == 0)
            return 0;
        return source << (64 - bitCount - bitOffset) >> (64 - bitCount);
    }

    public static void WriteBit(Span<byte> destination, int bitOffset, bool value)
    {
        int mask = 1 << (bitOffset & 7);

        if (value)
            destination[bitOffset >> 3] |= (byte)mask;
        // Our bit array is always initialized to 0, don't need to modify
        //else
        //    destination[bitOffset >> 3] &= (byte)~mask;
    }


    public static void WriteUInt8(Span<byte> destination, int bitOffset, byte value, int bitCount)
    {
        if (!ValidateArgs(destination.Length * 8, bitOffset, bitCount, 8))
            ThrowArgumentOutOfRangeException();

        if (bitCount + (bitOffset & 7) > 8)
        {
            ref ushort target = ref Unsafe.As<byte, ushort>(ref destination[bitOffset >> 3]);
            WriteValue16(ref target, bitOffset & 7, value, bitCount);
        }
        else
        {
            ref byte target = ref destination[bitOffset >> 3];
            WriteValue8(ref target, bitOffset & 7, value, bitCount);
        }
    }

    public static void WriteUInt32(Span<byte> destination, int bitOffset, uint value, int bitCount)
    {
        if (!ValidateArgs(destination.Length * 8, bitOffset, bitCount, 32))
            ThrowArgumentOutOfRangeException();

        if (bitCount + (bitOffset & 7) > 32)
        {
            // note: decomposing is ~18% faster on x86
            ref ulong target = ref Unsafe.As<byte, ulong>(ref destination[bitOffset >> 3]);
            WriteValue64(ref target, bitOffset & 7, value, bitCount);
        }
        else
        {
            ref uint target = ref Unsafe.As<byte, uint>(ref destination[bitOffset >> 3]);
            WriteValue32(ref target, bitOffset & 7, value, bitCount);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetMask(int size) => (1 << size) - 1;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteValue8(ref byte destination, int bitShift, int value, int bitCount)
    {
        if (bitCount == 0)
            return;

        destination = (byte)((destination & ~(GetMask(bitCount) << bitShift)) | ((value & GetMask(bitCount)) << bitShift));
    }

    private static void WriteValue16(ref ushort destination, int bitOffset, int value, int bitCount)
    {
        if (bitCount == 0)
            return;

        // decompose into two byte refs
        ref byte destLo = ref Unsafe.As<ushort, byte>(ref destination);
        ref byte destHi = ref Unsafe.Add(ref destLo, 1);

        int bitMask = GetMask(bitCount);

        value = (value & bitMask) << bitOffset;
        bitMask <<= bitOffset;

        destLo = (byte)((destLo & ~bitMask) | value);
        destHi = (byte)((destHi & ~(bitMask >> 8)) | (value >> 8));
    }

    private static void WriteValue32(ref uint destination, int bitOffset, uint value, int bitCount)
    {
        if (bitCount == 0)
            return;

        // create the mask
        uint mask = uint.MaxValue >> (32 - bitCount);

        // truncate the value
        value &= mask;

        // align to the correct bit
        mask <<= bitOffset;
        value <<= bitOffset;

        destination &= ~mask;
        destination |= value;
    }

    private static void WriteValue64(ref ulong destination, int bitOffset, ulong value, int bitCount)
    {
        if (bitCount == 0)
            return;

        // create the mask
        ulong mask = ulong.MaxValue >> (64 - bitCount);

        // truncate the value
        value &= mask;

        // align to the correct bit
        mask <<= bitOffset;
        value <<= bitOffset;

        destination &= ~mask;
        destination |= value;
    }
}
