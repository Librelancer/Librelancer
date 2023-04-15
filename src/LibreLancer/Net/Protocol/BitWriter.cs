using System;
using System.ComponentModel.DataAnnotations;
using System.Numerics;
using System.Runtime.InteropServices;

namespace LibreLancer.Net.Protocol
{
    public class BitWriter
    {
        private const int GROWTH_AMOUNT = 4;

        private byte[] buffer;
        private int bitOffset;

        public NetHpidWriter HpidWriter;

        public BitWriter(int initialCapacity = 64)
        {
            buffer = new byte [(initialCapacity + 7) >> 3];
            bitOffset = 0;
        }

        public void PutHpid(string hpid)
        {
            if (HpidWriter == null) throw new InvalidOperationException();
            if (hpid == null) PutVarUInt32(0);
            else if (hpid == "") PutVarUInt32(1);
            else PutVarUInt32(HpidWriter.GetIndex(hpid) + 2);
        }

        public void PutInt(int i) => PutUInt((uint) i, 32);
        public void PutByte(byte b) => PutUInt(b, 8);

        [StructLayout(LayoutKind.Explicit)]
        struct F2I
        {
            [FieldOffset(0)] public float f;
            [FieldOffset(0)] public uint i;
        }

        public void PutFloat(float f)
        {
            var c = new F2I() {f = f};
            PutUInt(c.i, 32);
        }

        public void PutVector3(Vector3 vec)
        {
            PutFloat(vec.X);
            PutFloat(vec.Y);
            PutFloat(vec.Z);
        }

        public void PutRadiansQuantized(float angle)
        {
            PutUInt(NetPacking.QuantizeAngle(angle, 16), 16);
        }

        public void PutVarInt32(int i)
        {
            PutVarInt64(i);
        }

        public void PutVarUInt32(uint u)
        {
            PutVarUInt64(u);
        }

        public void PutVarInt64(long value)
        {
            PutVarUInt64(NetPacking.Zig64(value));
        }

        public void PutVarUInt64(ulong u)
        {
            if (u <= 127)
            {
                PutByte((byte) u);
            }
            else if (u <= 16511)
            {
                u -= 128;
                PutByte((byte) ((u & 0x7f) | 0x80));
                PutByte((byte) ((u >> 7) & 0x7f));
            }
            else if (u <= 2113662)
            {
                u -= 16512;
                PutByte((byte) ((u & 0x7f) | 0x80));
                PutByte((byte) (((u >> 7) & 0x7f) | 0x80));
                PutByte((byte) ((u >> 14) & 0x7f));
            }
            else if (u <= 270549118)
            {
                u -= 2113663;
                PutByte((byte) ((u & 0x7f) | 0x80));
                PutByte((byte) (((u >> 7) & 0x7f) | 0x80));
                PutByte((byte) (((u >> 14) & 0x7f) | 0x80));
                PutByte((byte) ((u >> 21) & 0x7f));
            }
            else if (u <= 34630197486)
            {
                u -= 270549119;
                PutByte((byte) ((u & 0x7f) | 0x80));
                PutByte((byte) (((u >> 7) & 0x7f) | 0x80));
                PutByte((byte) (((u >> 14) & 0x7f) | 0x80));
                PutByte((byte) (((u >> 21) & 0x7f) | 0x80));
                PutByte((byte) ((u >> 28) & 0x7f));
            }
            else if (u <= 4432676708590)
            {
                u -= 34630197487;
                PutByte((byte) ((u & 0x7f) | 0x80));
                PutByte((byte) (((u >> 7) & 0x7f) | 0x80));
                PutByte((byte) (((u >> 14) & 0x7f) | 0x80));
                PutByte((byte) (((u >> 21) & 0x7f) | 0x80));
                PutByte((byte) (((u >> 28) & 0x7f) | 0x80));
                PutByte((byte) ((u >> 35) & 0x7f));
            }
            else if (u <= 567382630129902)
            {
                u -= 4432676708591;
                PutByte((byte) ((u & 0x7f) | 0x80));
                PutByte((byte) (((u >> 7) & 0x7f) | 0x80));
                PutByte((byte) (((u >> 14) & 0x7f) | 0x80));
                PutByte((byte) (((u >> 21) & 0x7f) | 0x80));
                PutByte((byte) (((u >> 28) & 0x7f) | 0x80));
                PutByte((byte) (((u >> 35) & 0x7f) | 0x80));
                PutByte((byte) ((u >> 42) & 0x7f));
            }
            else if (u <= 72624976668057838)
            {
                u -= 567382630129903;
                PutByte((byte) ((u & 0x7f) | 0x80));
                PutByte((byte) (((u >> 7) & 0x7f) | 0x80));
                PutByte((byte) (((u >> 14) & 0x7f) | 0x80));
                PutByte((byte) (((u >> 21) & 0x7f) | 0x80));
                PutByte((byte) (((u >> 28) & 0x7f) | 0x80));
                PutByte((byte) (((u >> 35) & 0x7f) | 0x80));
                PutByte((byte) (((u >> 42) & 0x7f) | 0x80));
                PutByte((byte) ((u >> 49) & 0x7f));
            }
            else
            {
                u -= 72624976668057839;
                PutByte((byte) ((u & 0x7f) | 0x80));
                PutByte((byte) (((u >> 7) & 0x7f) | 0x80));
                PutByte((byte) (((u >> 14) & 0x7f) | 0x80));
                PutByte((byte) (((u >> 21) & 0x7f) | 0x80));
                PutByte((byte) (((u >> 28) & 0x7f) | 0x80));
                PutByte((byte) (((u >> 35) & 0x7f) | 0x80));
                PutByte((byte) (((u >> 42) & 0x7f) | 0x80));
                PutByte((byte) (((u >> 49) & 0x7f) | 0x80));
                PutByte((byte) (u >> 57));
            }
        }


        public void PutNormal(Vector3 v)
        {
            v.Normalize();
            var maxIndex = 0;
            var maxValue = Math.Abs(v.X);
            bool sign = v.X < 0;
            if (Math.Abs(v.Y) > maxValue)
            {
                maxIndex = 1;
                maxValue = Math.Abs(v.Y);
                sign = v.Y < 0;
            }

            if (Math.Abs(v.Z) > maxValue)
            {
                maxIndex = 2;
                sign = v.Z < 0;
            }

            PutUInt((uint) maxIndex, 2);
            PutBool(sign);
            if (maxIndex == 0)
            {
                PutRangedFloat(v.Y, NetPacking.UNIT_MIN, NetPacking.UNIT_MAX, 14);
                PutRangedFloat(v.Z, NetPacking.UNIT_MIN, NetPacking.UNIT_MAX, 15);
            }

            if (maxIndex == 1)
            {
                PutRangedFloat(v.X, NetPacking.UNIT_MIN, NetPacking.UNIT_MAX, 14);
                PutRangedFloat(v.Z, NetPacking.UNIT_MIN, NetPacking.UNIT_MAX, 15);
            }

            if (maxIndex == 2)
            {
                PutRangedFloat(v.X, NetPacking.UNIT_MIN, NetPacking.UNIT_MAX, 14);
                PutRangedFloat(v.Y, NetPacking.UNIT_MIN, NetPacking.UNIT_MAX, 15);
            }
        }

        public void PutQuaternion(Quaternion q, int precision = NetPacking.BITS_COMPONENT)
        {
            NetPacking.PackQuaternion(q, precision, out var max, out var a, out var b, out var c);
            PutUInt(max, 2);
            PutUInt(a, precision);
            PutUInt(b, precision);
            PutUInt(c, precision);
        }

        public void PutBool(bool b)
        {
            CheckSize(bitOffset + 1);
            PackBits(b ? (byte) 1 : (byte) 0, 1, buffer, bitOffset++);
        }

        public void PutUInt(uint u, int bits)
        {
            CheckSize(bitOffset + bits);
            PackUInt(u, bits, buffer, bitOffset);
            bitOffset += bits;
        }

        public void PutRangedFloat(float f, float min, float max, int bits)
        {
            PutUInt(NetPacking.QuantizeFloat(f, min, max, bits), bits);
        }

        public void PutRangedVector3(Vector3 v, float min, float max, int bits)
        {
            PutRangedFloat(v.X, min, max, bits);
            PutRangedFloat(v.Y, min, max, bits);
            PutRangedFloat(v.Z, min, max, bits);
        }

        static void PackUInt(uint src, int nBits, Span<byte> dest, int destOffset)
        {
            if (nBits <= 8)
            {
                PackBits((byte) src, nBits, dest, destOffset);
                return;
            }

            PackBits((byte) src, 8, dest, destOffset);
            destOffset += 8;
            nBits -= 8;
            if (nBits <= 8)
            {
                PackBits((byte) (src >> 8), nBits, dest, destOffset);
                return;
            }

            PackBits((byte) (src >> 8), 8, dest, destOffset);
            destOffset += 8;
            nBits -= 8;
            if (nBits <= 8)
            {
                PackBits((byte) (src >> 16), nBits, dest, destOffset);
                return;
            }

            PackBits((byte) (src >> 16), 8, dest, destOffset);
            destOffset += 8;
            nBits -= 8;
            PackBits((byte) (src >> 24), nBits, dest, destOffset);
        }

        static void PackBits(byte src, int nBits, Span<byte> dest, int destOffset)
        {
            src = (byte) (src & (0xFF >> (8 - nBits)));
            int p = destOffset >> 3;
            int bitsUsed = destOffset & 0x7;
            int bitsFree = 8 - bitsUsed;
            int bitsLeft = bitsFree - nBits;
            if (bitsLeft >= 0)
            {
                int mask = (0xFF >> bitsFree) | (0xFF << (8 - bitsLeft));
                dest[p] = (byte) (
                    (dest[p] & mask) |
                    (src << bitsUsed));
                return;
            }

            dest[p] = (byte) (
                (dest[p] & (0xFF >> bitsFree)) |
                (src << bitsUsed)
            );
            p++;
            dest[p] = (byte) (
                (dest[p] & (0xFF << (nBits - bitsFree))) |
                (src >> bitsFree)
            );
        }

        void CheckSize(int nBits)
        {
            int byteLen = (nBits + 7) >> 3;
            if (buffer.Length < byteLen)
                Array.Resize(ref buffer, byteLen + GROWTH_AMOUNT);
        }

        public bool NeedsNewByte(int bitCount)
        {
            return bitCount >= 8 || (8 - (bitOffset & 0x7)) < bitCount;
        }

        public byte[] GetBuffer()
        {
            var b = new byte[ByteLength];
            for (int i = 0; i < ByteLength; i++) b[i] = buffer[i];
            return b;
        }

        public int ByteLength => (bitOffset + 7) >> 3;

        public void WriteToPacket(PacketWriter dw)
        {
            dw.Put(buffer, 0, (bitOffset + 7) >> 3);
        }
    }
}