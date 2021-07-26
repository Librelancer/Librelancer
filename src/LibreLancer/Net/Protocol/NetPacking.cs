// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace LibreLancer
{
	public static class NetPacking
	{
        public const int BITS_COMPONENT = 10;
        public const float UNIT_MIN = -0.707107f;
        public const float UNIT_MAX = 0.707107f;
        public const float ANGLE_MIN = (float)(-2 * Math.PI);
        public const float ANGLE_MAX = (float)(2 * Math.PI);
        
        public static void PutVariableUInt64(this LiteNetLib.Utils.NetDataWriter writer, ulong value)
        {
            ulong num1 = value;
            while (num1 >= 0x80)
            {
                writer.Put((byte)(num1 | 0x80));
                num1 = num1 >> 7;
            }
            writer.Put((byte)num1);
        }
        
        public static ulong GetVariableUInt64(this LiteNetLib.Utils.NetDataReader reader)
        {
            ulong num1 = 0;
            int num2 = 0;
            while (reader.AvailableBytes > 0)
            {
                byte num3 = reader.GetByte();
                num1 |= (ulong)(num3 & 0x7f) << num2;
                num2 += 7;
                if ((num3 & 0x80) == 0)
                    return num1;
            }
            throw new Exception("Malformed variable UInt32");
        }
        
        public static void PutVariableUInt32(this LiteNetLib.Utils.NetDataWriter writer, uint value)
        {
            uint num1 = value;
            while (num1 >= 0x80)
            {
                writer.Put((byte)(num1 | 0x80));
                num1 = num1 >> 7;
            }
            writer.Put((byte)num1);
        }

        public static uint GetVariableUInt32(this LiteNetLib.Utils.NetDataReader reader)
        {
            int num1 = 0;
            int num2 = 0;
            while (reader.AvailableBytes > 0)
            {
                byte num3 = reader.GetByte();
                num1 |= (num3 & 0x7f) << num2;
                num2 += 7;
                if ((num3 & 0x80) == 0)
                    return (uint)num1;
            }
            throw new Exception("Malformed variable UInt32");
        }

        public static void PutStringPacked(this LiteNetLib.Utils.NetDataWriter om, string s)
        {
            if (s == null) {
                om.PutVariableUInt32(0); 
            }
            else {
                var bytes = Encoding.UTF8.GetBytes(s);
                om.PutVariableUInt32((uint)(bytes.Length + 1));
                om.Put(bytes);
            }
        }
        public static string GetStringPacked(this LiteNetLib.Utils.NetDataReader im)
        {
            var len = im.GetVariableUInt32();
            if (len == 0) return null;
            len--;
            var bytes = im.GetBytes((int) len);
            return Encoding.UTF8.GetString(bytes);
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
