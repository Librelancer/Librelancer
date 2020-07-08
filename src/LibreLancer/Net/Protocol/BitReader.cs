// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using System.Runtime.InteropServices;
using System.Numerics;

namespace LibreLancer
{
    public ref struct BitReader
    {
        private ReadOnlySpan<byte> array;
        private int bitsOffset;

        public BitReader(ReadOnlySpan<byte> array, int bitsOffset)
        {
            this.array = array;
            this.bitsOffset = bitsOffset;
        }

        public int GetInt()
        {
            return (int) GetUInt();
        }
        
        [StructLayout(LayoutKind.Explicit)]
        struct F2I
        {
            [FieldOffset(0)] public float f;
            [FieldOffset(0)] public uint i;
        }
        public float GetFloat()
        {
            var c = new F2I() {i = GetUInt()};
            return c.f;
        }
        public Vector3 GetVector3()
        {
            return new Vector3(GetFloat(), GetFloat(), GetFloat());
        }
        
        public Quaternion GetQuaternion()
        {
            var maxIndex = (int) GetUInt(2);
            var a = GetRangedFloat(NetPacking.UNIT_MIN, NetPacking.UNIT_MAX, NetPacking.BITS_COMPONENT);
            var b = GetRangedFloat(NetPacking.UNIT_MIN, NetPacking.UNIT_MAX, NetPacking.BITS_COMPONENT);
            var c = GetRangedFloat(NetPacking.UNIT_MIN, NetPacking.UNIT_MAX, NetPacking.BITS_COMPONENT);
            var d = (float)Math.Sqrt(1f - (a * a + b * b + c * c));
            #if DEBUG
            if (float.IsNaN(a) || float.IsNaN(b) || float.IsNaN(c) || float.IsNaN(d))
            {
                throw new Exception("Degenerate quaternion. Check alignment?");
            }
            #endif
            Quaternion q;
            if (maxIndex == 0)
                return new Quaternion(d, a, b, c);
            if (maxIndex == 1)
                return new Quaternion(a, d, b, c);
            if (maxIndex == 2)
                return new Quaternion(a, b, d, c);
            return new Quaternion(a, b, c, d);
        }
        
        public uint GetUInt(int bits = 32)
        {
            if(bits <= 0 || bits > 32)
                throw new ArgumentOutOfRangeException();
            var retval = UnpackUInt(array, bits, bitsOffset);
            bitsOffset += bits;
            return retval;
        }

        public float GetRangedFloat(float min, float max, int bits)
        {
            var u =  GetUInt(bits) / (float) ((1 << bits) - 1);
            return (min + (u * (max - min)));
        }
        public float GetRadiansQuantized()
        {
            return GetRangedFloat(NetPacking.ANGLE_MIN, NetPacking.ANGLE_MAX, 16);
        }
        public byte GetByte()
        {
            var b = UnpackBits(array, 8, bitsOffset);
            bitsOffset += 8;
            return b;
        }
        public bool GetBool()
        {
            return UnpackBits(array, 1, bitsOffset++) != 0;
        }

        static uint UnpackUInt(ReadOnlySpan<byte> buffer, int nBits, int readOffset)
        {
            //Byte 1
            uint retval;
            if (nBits <= 8) {
                return UnpackBits(buffer, nBits, readOffset);
            }
            retval = UnpackBits(buffer, 8, readOffset);
            nBits -= 8;
            readOffset += 8;
            //Byte 2
            if (nBits <= 8) {
                return retval | (uint) (UnpackBits(buffer, nBits, readOffset) << 8);
            }
            retval |= (uint) (UnpackBits(buffer, 8, readOffset) << 8);
            nBits -= 8;
            readOffset += 8;
            //Byte 3
            if (nBits <= 8) {
                return retval | (uint) (UnpackBits(buffer, nBits, readOffset) << 16);
            }
            retval |= (uint) (UnpackBits(buffer, nBits, readOffset) << 16);
            nBits -= 8;
            readOffset += 8;
            //Byte 4
            return retval | (uint)(UnpackBits(buffer, nBits, readOffset) << 24);
        }
        static byte UnpackBits(ReadOnlySpan<byte> buffer, int nBits, int readOffset)
        {
            int bytePtr = readOffset >> 3;
            int startIndex = readOffset - (bytePtr * 8);
            if (startIndex == 0 && nBits == 8)
                return buffer[bytePtr];

            byte returnValue = (byte) (buffer[bytePtr] >> startIndex);
            var remainingBits = nBits - (8 - startIndex);
            if (remainingBits < 1)
            {
                //Mask out
                return (byte) (returnValue & (0xFF >> (8 - nBits)));
            }

            byte second = buffer[bytePtr + 1];
            second &= (byte) (255 >> (8 - remainingBits));
            return (byte) (returnValue | (byte) (second << (nBits - remainingBits)));
        }
    }
}