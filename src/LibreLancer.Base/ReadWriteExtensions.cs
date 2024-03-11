using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace LibreLancer;

public static class ReadWriteExtensions
{
    public static void WriteInt32BE(this BinaryWriter writer, int val)
    {
        if (BitConverter.IsLittleEndian)
            writer.Write(BinaryPrimitives.ReverseEndianness(val));
        else
            writer.Write(val);
    }

    public static int ReadInt32BE(this BinaryReader reader)
    {
        return BitConverter.IsLittleEndian
            ? BinaryPrimitives.ReverseEndianness(reader.ReadInt32())
            : reader.ReadInt32();
    }

    public static void Skip(this BinaryReader reader, int size)
    {
        reader.BaseStream.Seek(size, SeekOrigin.Current);
    }

    public static unsafe void WriteStruct<T>(this BinaryWriter writer, T value) where T : unmanaged
    {
        Span<byte> bytes = stackalloc byte[Marshal.SizeOf<T>()];
        fixed (byte* ptr = &bytes.GetPinnableReference())
            Marshal.StructureToPtr<T>(value, (IntPtr)ptr, false);
        writer.Write(bytes);
    }

    public static unsafe T ReadStruct<T>(this BinaryReader reader) where T : unmanaged
    {
        Span<byte> spanBytes = stackalloc byte[Marshal.SizeOf<T>()];
        reader.Read(spanBytes);
        fixed (byte* ptr = &spanBytes.GetPinnableReference())
            return Marshal.PtrToStructure<T>((IntPtr)ptr);
    }

    public static uint ReadUInt24(this BinaryReader reader)
    {
        return (uint)reader.ReadByte() + ((uint)reader.ReadByte() << 8) + ((uint)reader.ReadByte() << 16);
    }

    public static void WriteStringUTF8(this BinaryWriter writer, string s)
    {
        if (s == null)
        {
            writer.Write((byte)0);
        }
        else
        {
            WriteVarUInt32(writer, (uint)(s.Length + 1));
            var bytes = Encoding.UTF8.GetBytes(s);
            writer.Write(bytes);
        }
    }

    public static string ReadStringUTF8(this BinaryReader reader)
    {
        var len = ReadVarUInt32(reader);
        if (len == 0)
        {
            return null;
        }

        var bytes = reader.ReadBytes((int)(len - 1));
        return Encoding.UTF8.GetString(bytes);
    }

    public static int ReadVarInt32(this BinaryReader reader)
    {
        return (int)ReadVarInt64(reader);
    }

    public static uint ReadVarUInt32(this BinaryReader reader)
    {
        return (uint)ReadVarUInt64(reader);
    }

    public static long ReadVarInt64(this BinaryReader reader)
    {
        return Zag64(ReadVarUInt64(reader));
    }

    public static ulong ReadVarUInt64(this BinaryReader reader)
    {
        long b = reader.ReadByte();
        var a = (ulong)(b & 0x7f);
        var extraCount = 0;
        //first extra
        if ((b & 0x80) == 0x80)
        {
            b = reader.ReadByte();
            a |= (uint)((b & 0x7f) << 7);
            extraCount++;
        }

        //second extra
        if ((b & 0x80) == 0x80)
        {
            b = reader.ReadByte();
            a |= (uint)((b & 0x7f) << 14);
            extraCount++;
        }

        //third extra
        if ((b & 0x80) == 0x80)
        {
            b = reader.ReadByte();
            a |= (uint)((b & 0x7f) << 21);
            extraCount++;
        }

        //fourth extra
        if ((b & 0x80) == 0x80)
        {
            b = reader.ReadByte();
            a |= (ulong)((b & 0x7f) << 28);
            extraCount++;
        }

        //fifth extra
        if ((b & 0x80) == 0x80)
        {
            b = reader.ReadByte();
            a |= (ulong)((b & 0x7f) << 35);
            extraCount++;
        }

        //sixth extra
        if ((b & 0x80) == 0x80)
        {
            b = reader.ReadByte();
            a |= (ulong)((b & 0x7f) << 42);
            extraCount++;
        }

        //seventh extra
        if ((b & 0x80) == 0x80)
        {
            b = reader.ReadByte();
            a |= (ulong)((b & 0x7f) << 49);
            extraCount++;
        }

        //Full ulong
        if ((b & 0x80) == 0x80)
        {
            b = reader.ReadByte();
            a |= (ulong)b << 57;
            extraCount++;
        }

        switch (extraCount)
        {
            case 1:
                a += 128;
                break;
            case 2:
                a += 16512;
                break;
            case 3:
                a += 2113663;
                break;
            case 4:
                a += 270549119;
                break;
            case 5:
                a += 34630197487;
                break;
            case 6:
                a += 4432676708591;
                break;
            case 7:
                a += 567382630129903;
                break;
            case 8:
                a += 72624976668057839;
                break;
        }

        return a;
    }

    public static void WriteVarInt32(this BinaryWriter writer, int value)
    {
        WriteVarInt64(writer, value);
    }

    public static void WriteVarUInt32(this BinaryWriter writer, uint value)
    {
        WriteVarUInt64(writer, value);
    }

    public static void WriteVarInt64(this BinaryWriter writer, long value)
    {
        WriteVarUInt64(writer, Zig64(value));
    }

    public static void WriteVarUInt64(this BinaryWriter writer, ulong u)
    {
        if (u <= 127)
        {
            writer.Write((byte)u);
        }
        else if (u <= 16511)
        {
            u -= 128;
            writer.Write((byte)((u & 0x7f) | 0x80));
            writer.Write((byte)((u >> 7) & 0x7f));
        }
        else if (u <= 2113662)
        {
            u -= 16512;
            writer.Write((byte)((u & 0x7f) | 0x80));
            writer.Write((byte)(((u >> 7) & 0x7f) | 0x80));
            writer.Write((byte)((u >> 14) & 0x7f));
        }
        else if (u <= 270549118)
        {
            u -= 2113663;
            writer.Write((byte)((u & 0x7f) | 0x80));
            writer.Write((byte)(((u >> 7) & 0x7f) | 0x80));
            writer.Write((byte)(((u >> 14) & 0x7f) | 0x80));
            writer.Write((byte)((u >> 21) & 0x7f));
        }
        else if (u <= 34630197486)
        {
            u -= 270549119;
            writer.Write((byte)((u & 0x7f) | 0x80));
            writer.Write((byte)(((u >> 7) & 0x7f) | 0x80));
            writer.Write((byte)(((u >> 14) & 0x7f) | 0x80));
            writer.Write((byte)(((u >> 21) & 0x7f) | 0x80));
            writer.Write((byte)((u >> 28) & 0x7f));
        }
        else if (u <= 4432676708590)
        {
            u -= 34630197487;
            writer.Write((byte)((u & 0x7f) | 0x80));
            writer.Write((byte)(((u >> 7) & 0x7f) | 0x80));
            writer.Write((byte)(((u >> 14) & 0x7f) | 0x80));
            writer.Write((byte)(((u >> 21) & 0x7f) | 0x80));
            writer.Write((byte)(((u >> 28) & 0x7f) | 0x80));
            writer.Write((byte)((u >> 35) & 0x7f));
        }
        else if (u <= 567382630129902)
        {
            u -= 4432676708591;
            writer.Write((byte)((u & 0x7f) | 0x80));
            writer.Write((byte)(((u >> 7) & 0x7f) | 0x80));
            writer.Write((byte)(((u >> 14) & 0x7f) | 0x80));
            writer.Write((byte)(((u >> 21) & 0x7f) | 0x80));
            writer.Write((byte)(((u >> 28) & 0x7f) | 0x80));
            writer.Write((byte)(((u >> 35) & 0x7f) | 0x80));
            writer.Write((byte)((u >> 42) & 0x7f));
        }
        else if (u <= 72624976668057838)
        {
            u -= 567382630129903;
            writer.Write((byte)((u & 0x7f) | 0x80));
            writer.Write((byte)(((u >> 7) & 0x7f) | 0x80));
            writer.Write((byte)(((u >> 14) & 0x7f) | 0x80));
            writer.Write((byte)(((u >> 21) & 0x7f) | 0x80));
            writer.Write((byte)(((u >> 28) & 0x7f) | 0x80));
            writer.Write((byte)(((u >> 35) & 0x7f) | 0x80));
            writer.Write((byte)(((u >> 42) & 0x7f) | 0x80));
            writer.Write((byte)((u >> 49) & 0x7f));
        }
        else
        {
            u -= 72624976668057839;
            writer.Write((byte)((u & 0x7f) | 0x80));
            writer.Write((byte)(((u >> 7) & 0x7f) | 0x80));
            writer.Write((byte)(((u >> 14) & 0x7f) | 0x80));
            writer.Write((byte)(((u >> 21) & 0x7f) | 0x80));
            writer.Write((byte)(((u >> 28) & 0x7f) | 0x80));
            writer.Write((byte)(((u >> 35) & 0x7f) | 0x80));
            writer.Write((byte)(((u >> 42) & 0x7f) | 0x80));
            writer.Write((byte)(((u >> 49) & 0x7f) | 0x80));
            writer.Write((byte)(u >> 57));
        }
    }


    private static ulong Zig64(long value)
    {
        return (ulong)((value << 1) ^ (value >> 63));
    }

    private static long Zag64(ulong ziggedValue)
    {
        const long Int64Msb = 1L << 63;
        var value = (long)ziggedValue;
        return -(value & 0x01) ^ ((value >> 1) & ~Int64Msb);
    }
}
