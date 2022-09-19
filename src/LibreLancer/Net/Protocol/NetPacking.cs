// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace LibreLancer
{
	public static partial class NetPacking
	{
        public const int BITS_COMPONENT = 10;
        public const float UNIT_MIN = -0.70710677f;
        public const float UNIT_MAX = 0.70710677f;
        public const float ANGLE_MIN = (float)(-2 * Math.PI);
        public const float ANGLE_MAX = (float)(2 * Math.PI);

        
        public static void PutVariableUInt64(this LiteNetLib.Utils.NetDataWriter writer, ulong u)
        {
            if (u <= 127) {
                writer.Put((byte)u);
            } 
            else if (u <= 16511) {
                u -= 128;
                writer.Put((byte)((u & 0x7f) | 0x80));
                writer.Put((byte)((u >> 7) & 0x7f));
            } 
            else if (u <= 2113662) {
                u -= 16512;
                writer.Put((byte)((u & 0x7f) | 0x80));
                writer.Put((byte) (((u >> 7) & 0x7f) | 0x80));
                writer.Put((byte)((u >> 14) & 0x7f));
            } 
            else if (u <= 270549118) {
                u -= 2113663;
                writer.Put((byte)((u & 0x7f) | 0x80));
                writer.Put((byte)(((u >> 7) & 0x7f) | 0x80));
                writer.Put((byte)(((u >> 14) & 0x7f) | 0x80));
                writer.Put((byte)((u >> 21) & 0x7f));
            }
            else if (u <= 34630197486) {
                u -= 270549119;
                writer.Put((byte)((u & 0x7f) | 0x80));
                writer.Put((byte)(((u >> 7) & 0x7f) | 0x80));
                writer.Put((byte)(((u >> 14) & 0x7f) | 0x80));
                writer.Put((byte)(((u >> 21) & 0x7f) | 0x80));
                writer.Put((byte)((u >> 28) & 0x7f));
            }
            else if (u <= 4432676708590) {
                u -= 34630197487;
                writer.Put((byte)((u & 0x7f) | 0x80));
                writer.Put((byte)(((u >> 7) & 0x7f) | 0x80));
                writer.Put((byte)(((u >> 14) & 0x7f) | 0x80));
                writer.Put((byte)(((u >> 21) & 0x7f) | 0x80));
                writer.Put((byte)(((u >> 28) & 0x7f) | 0x80));
                writer.Put((byte)((u >> 35) & 0x7f));
            }
            else if (u <= 567382630129902) {
                u -= 4432676708591;
                writer.Put((byte)((u & 0x7f) | 0x80));
                writer.Put((byte)(((u >> 7) & 0x7f) | 0x80));
                writer.Put((byte)(((u >> 14) & 0x7f) | 0x80));
                writer.Put((byte)(((u >> 21) & 0x7f) | 0x80));
                writer.Put((byte)(((u >> 28) & 0x7f) | 0x80));
                writer.Put((byte)(((u >> 35) & 0x7f) | 0x80));
                writer.Put((byte)((u >> 42) & 0x7f));
            }
            else if (u <= 72624976668057838) {
                u -= 567382630129903;
                writer.Put((byte)((u & 0x7f) | 0x80));
                writer.Put((byte)(((u >> 7) & 0x7f) | 0x80));
                writer.Put((byte)(((u >> 14) & 0x7f) | 0x80));
                writer.Put((byte)(((u >> 21) & 0x7f) | 0x80));
                writer.Put((byte)(((u >> 28) & 0x7f) | 0x80));
                writer.Put((byte)(((u >> 35) & 0x7f) | 0x80));
                writer.Put((byte)(((u >> 42) & 0x7f) | 0x80));
                writer.Put((byte)((u >> 49) & 0x7f));
            }
            else
            {
                u -= 72624976668057839;
                writer.Put((byte)((u & 0x7f) | 0x80));
                writer.Put((byte)(((u >> 7) & 0x7f) | 0x80));
                writer.Put((byte)(((u >> 14) & 0x7f) | 0x80));
                writer.Put((byte)(((u >> 21) & 0x7f) | 0x80));
                writer.Put((byte)(((u >> 28) & 0x7f) | 0x80));
                writer.Put((byte)(((u >> 35) & 0x7f) | 0x80));
                writer.Put((byte)(((u >> 42) & 0x7f) | 0x80));
                writer.Put((byte)(((u >> 49) & 0x7f) | 0x80));
                writer.Put((byte)(u >> 57));
            }
        }
        
        public static ulong GetVariableUInt64(this LiteNetLib.Utils.NetDataReader reader)
        {
            long b = reader.GetByte();
            ulong a = (ulong) (b & 0x7f);
            int extraCount = 0;
            //first extra
            if ((b & 0x80) == 0x80)
            {
                b = reader.GetByte();
                a |= (uint) ((b & 0x7f) << 7);
                extraCount++;
            }
            //second extra
            if ((b & 0x80) == 0x80)
            {
                b = reader.GetByte();
                a |= (uint) ((b & 0x7f) << 14);
                extraCount++;
            }
            //third extra
            if ((b & 0x80) == 0x80)
            {
                b = reader.GetByte();
                a |= (uint) ((b & 0x7f) << 21);
                extraCount++;
            }
            //fourth extra
            if ((b & 0x80) == 0x80)
            {
                b = reader.GetByte();
                a |= (ulong) ((b & 0x7f) << 28);
                extraCount++;
            }
            //fifth extra
            if ((b & 0x80) == 0x80)
            {
                b = reader.GetByte();
                a |= (ulong) ((b & 0x7f) << 35);
                extraCount++;
            }
            //sixth extra
            if ((b & 0x80) == 0x80)
            {
                b = reader.GetByte();
                a |= (ulong) ((b & 0x7f) << 42);
                extraCount++;
            }
            //seventh extra
            if ((b & 0x80) == 0x80)
            {
                b = reader.GetByte();
                a |= (ulong) ((b & 0x7f) << 49);
                extraCount++;
            }
            //Full ulong
            if ((b & 0x80) == 0x80)
            {
                b = reader.GetByte();
                a |= (ulong) (((ulong)b) << 57);
                extraCount++;
            }
            switch (extraCount) {
                case 1: a += 128; break;
                case 2: a += 16512; break;
                case 3: a += 2113663; break;
                case 4: a += 270549119; break;
                case 5: a += 34630197487; break;
                case 6: a += 4432676708591; break;
                case 7: a += 567382630129903; break;
                case 8: a += 72624976668057839; break;
            }
            return a;
        }

        public static ulong Zig64(long value)
        {
            return (ulong)((value << 1) ^ (value >> 63));
        }

        public static long Zag64(ulong ziggedValue)
        {
            const long Int64Msb = 1L << 63;
            long value = (long)ziggedValue;
            return (-(value & 0x01)) ^ ((value >> 1) & ~Int64Msb);
        }

        public static uint Zig32(int value)
        {
            return (uint)((value << 1) ^ (value >> 31));
        }
        
        public static int Zag32(uint ziggedValue)
        {
            const int Int32Msb = 1 << 31;
            int value = (int)ziggedValue;
            return (-(value & 0x01)) ^ ((value >> 1) & ~Int32Msb);
        }

        public static void PutVariableInt64(this LiteNetLib.Utils.NetDataWriter writer, long value)
        {
            PutVariableUInt64(writer, Zig64(value));
        }

        public static long GetVariableInt64(this LiteNetLib.Utils.NetDataReader reader)
        {
            return Zag64(GetVariableUInt64(reader));
        }
        
        public static void PutVariableInt32(this LiteNetLib.Utils.NetDataWriter writer, int value)
        {
            PutVariableUInt32(writer, Zig32(value));
        }
        
        public static int GetVariableInt32(this LiteNetLib.Utils.NetDataReader reader)
        {
            return Zag32(GetVariableUInt32(reader));
        }
        
        public static void PutVariableUInt32(this LiteNetLib.Utils.NetDataWriter writer, uint u)
        {
            PutVariableUInt64(writer, u);
        }

        public static bool TryPeekVariableUInt32(this LiteNetLib.Utils.NetDataReader reader, ref int offset, out uint len)
        {
            len = 0;
            bool TryPeekByte(ref int o, out byte v)
            {
                v = 0;
                if (reader.AvailableBytes < o) return false;
                v = reader.RawData[reader.Position + o++];
                return true;
            }
            uint a = 0;
            if (!TryPeekByte(ref offset, out byte b)) return false;
            a = (uint) (b & 0x7f);
            int extraCount = 0;
            //first extra
            if ((b & 0x80) == 0x80)
            {
                if (!TryPeekByte(ref offset, out b)) return false;
                a |= (uint) ((b & 0x7f) << 7);
                extraCount++;
            }
            //second extra
            if ((b & 0x80) == 0x80)
            {
                if (!TryPeekByte(ref offset, out b)) return false;
                a |= (uint) ((b & 0x7f) << 7);
                extraCount++;
            }
            //third extra
            if ((b & 0x80) == 0x80)
            {
                if (!TryPeekByte(ref offset, out b)) return false;
                a |= (uint) ((b & 0x7f) << 7);
                extraCount++;
            }
            //fourth extra
            if ((b & 0x80) == 0x80)
            {
                if (!TryPeekByte(ref offset, out b)) return false;
                a |= (uint) ((b & 0x7f) << 7);
                extraCount++;
            }
            switch (extraCount) {
                case 1: a += 128; break;
                case 2: a += 16512; break;
                case 3: a += 2113663; break;
            }
            len = a;
            return true;
        }

        public static uint GetVariableUInt32(this LiteNetLib.Utils.NetDataReader reader)
        {
            return (uint) GetVariableUInt64(reader);
        }

        public static unsafe void Put(this LiteNetLib.Utils.NetDataWriter om, Guid g)
        {
            var longs = (ulong*)&g;
            om.Put(longs[0]);
            om.Put(longs[1]);
        }

        public static unsafe Guid GetGuid(this LiteNetLib.Utils.NetDataReader im)
        {
            Guid g = new Guid();
            var longs = (ulong*) &g;
            longs[0] = im.GetULong();
            longs[1] = im.GetULong();
            return g;
        }
        
		public static void Put(this LiteNetLib.Utils.NetDataWriter om, Quaternion q)
        {
            var pack = new BitWriter(32);
            pack.PutQuaternion(q);
            Debug.Assert(pack.ByteLength == 4);
            pack.WriteToPacket(om);
        }
        
        public static Quaternion GetQuaternion(this LiteNetLib.Utils.NetDataReader im)
        {
            var buf = new byte[4];
            im.GetBytes(buf, 4);
            var pack = new BitReader(buf, 0);
            return pack.GetQuaternion();
        }
        
        public static void PutNormal(this LiteNetLib.Utils.NetDataWriter om, Vector3 n)
        {
            var pack = new BitWriter(32);
            pack.PutNormal(n);
            Debug.Assert(pack.ByteLength == 4);
            pack.WriteToPacket(om);
        }

        public static Vector3 GetNormal(this LiteNetLib.Utils.NetDataReader im)
        {
            var buf = new byte[4];
            im.GetBytes(buf, 4);
            var pack = new BitReader(buf, 0);
            return pack.GetNormal();
        }
            

		public static void Put(this LiteNetLib.Utils.NetDataWriter om, Vector3 vec)
		{
            om.Put(vec.X);
            om.Put(vec.Y);
            om.Put(vec.Z);
		}

        public static Vector3 GetVector3(this LiteNetLib.Utils.NetDataReader im)
        {
            return new Vector3(im.GetFloat(), im.GetFloat(), im.GetFloat());
        }

        public static byte[] GetBytes(this LiteNetLib.Utils.NetDataReader im, int count)
        {
            var buf = new byte[count];
            im.GetBytes(buf, count);
            return buf;
        }
    }
}
