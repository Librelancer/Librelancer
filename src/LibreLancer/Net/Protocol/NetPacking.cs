// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;

namespace LibreLancer.Net.Protocol
{
	public static partial class NetPacking
	{
        public const int BITS_COMPONENT = 10;
        public const float UNIT_MIN = -0.707106829f;
        public const float UNIT_MAX = 0.707106829f;
        public const float ANGLE_MIN = (float)(-2 * Math.PI);
        public const float ANGLE_MAX = (float)(2 * Math.PI);

        public static int ByteCountUInt64(ulong u)
        {
            return u switch
            {
                <= 127 => 1,
                <= 16511 => 2,
                <= 2113662 => 3,
                <= 270549118 => 4,
                <= 34630197486 => 5,
                <= 4432676708590 => 6,
                <= 567382630129902 => 7,
                <= 72624976668057838 => 8,
                _ => 9
            };
        }

        public static int ByteCountInt64(long l) => ByteCountUInt64(Zig64(l));

        public static uint Zig32(int value)
        {
            return (uint)((value << 1) ^ (value >> 31));
        }

        public static int Zag32(uint value)
        {
            return (int)((value >> 1) ^ (int)(-(value&1)));
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

        public static uint QuantizeFloat(float f, float min, float max, int bits)
        {
            #if DEBUG
            if (f < min || f > max) throw new ArgumentOutOfRangeException();
            #else
            f = MathHelper.Clamp(f, min, max);
            #endif
            var intMax = (1 << bits) - 1;
            float unit = ((f - min) / (max - min));
            return (uint) (intMax * unit);
        }

        public static float UnquantizeFloat(uint i, float min, float max, int bits)
        {
            var u =  i / (float) ((1 << bits) - 1);
            return (min + (u * (max - min)));
        }

        public static uint ApplyDelta(uint d, uint src, int deltaBits)
        {
            var deltaOffset = 2 << (deltaBits - 2);
            var diff = ((int) d) - deltaOffset;
            return (uint) ((int) src + diff);
        }

        public static bool TryDelta(uint a, uint src, int deltaBits, out uint delta)
        {
            var deltaOffset = 2 << (deltaBits - 2);
            var deltaMin = -deltaOffset;
            var deltaMax = deltaOffset - 1;
            var diff = (int) a - (int) src;
            if (diff >= deltaMin && diff <= deltaMax) {
                delta = (uint) (diff + deltaOffset);
                return true;
            }
            delta = 0;
            return false;
        }

        public static Quaternion UnpackQuaternion(int precision, uint max, uint a, uint b, uint c)
        {
            var fa = UnquantizeFloat(a, UNIT_MIN, UNIT_MAX, precision);
            var fb = UnquantizeFloat(b, UNIT_MIN, UNIT_MAX, precision);
            var fc = UnquantizeFloat(c, UNIT_MIN, UNIT_MAX, precision);
            var d = (float)Math.Sqrt(1f - (fa * fa + fb * fb + fc * fc));
#if DEBUG
            if (float.IsNaN(fa) || float.IsNaN(fb) || float.IsNaN(fc) || float.IsNaN(d))
            {
                throw new Exception("Degenerate quaternion. Check alignment?");
            }
#endif
            if (max == 0)
                return new Quaternion(d, fa, fb, fc);
            if (max == 1)
                return new Quaternion(fa, d, fb, fc);
            if (max == 2)
                return new Quaternion(fa, fb, d, fc);
            return new Quaternion(fa, fb, fc, d);
        }

        public static void PackQuaternion(Quaternion q, int precision, out uint maxIndex, out uint a, out uint b, out uint c)
        {
            maxIndex = 0;
            var maxValue = Math.Abs(q.X);
            var sign = 1f;
            sign = q.X < 0 ? -1 : 1;
            if (Math.Abs(q.Y) > maxValue)
            {
                maxValue = Math.Abs(q.Y);
                maxIndex = 1;
                sign = q.Y < 0 ? -1 : 1;
            }
            if (Math.Abs(q.Z) > maxValue)
            {
                maxValue = Math.Abs(q.Z);
                maxIndex = 2;
                sign = q.Z < 0 ? -1 : 1;
            }
            if (Math.Abs(q.W) > maxValue)
            {
                maxIndex = 3;
                sign = q.W < 0 ? -1 : 1;
            }
            if (maxIndex == 0)
            {
                a = QuantizeFloat(q.Y * sign, UNIT_MIN, UNIT_MAX, precision);
                b = QuantizeFloat(q.Z * sign, UNIT_MIN, UNIT_MAX, precision);
                c = QuantizeFloat(q.W * sign, UNIT_MIN, UNIT_MAX, precision);
            }
            else if (maxIndex == 1)
            {
                a = QuantizeFloat(q.X * sign, UNIT_MIN, UNIT_MAX, precision);
                b = QuantizeFloat(q.Z * sign, UNIT_MIN, UNIT_MAX, precision);
                c = QuantizeFloat(q.W * sign, UNIT_MIN, UNIT_MAX, precision);
            }
            else if (maxIndex == 2)
            {
                a = QuantizeFloat(q.X * sign, UNIT_MIN, UNIT_MAX, precision);
                b = QuantizeFloat(q.Y * sign, UNIT_MIN, UNIT_MAX, precision);
                c = QuantizeFloat(q.W * sign, UNIT_MIN, UNIT_MAX, precision);
            }
            else
            {
                a = QuantizeFloat(q.X * sign, UNIT_MIN, UNIT_MAX, precision);
                b = QuantizeFloat(q.Y * sign, UNIT_MIN, UNIT_MAX, precision);
                c = QuantizeFloat(q.Z * sign, UNIT_MIN, UNIT_MAX, precision);
            }
            #if DEBUG
            UnpackQuaternion(precision, maxIndex, a, b, c);
            #endif
        }

        private static float WrapMinMax(float x, float min, float max)
        {
            var m = max - min;
            var y = (x - min);
            return min + (m + y % m) % m;
        }

        public static uint QuantizeAngle(float angle, int bits)
        {
            var wrapped = WrapMinMax(angle, ANGLE_MIN, ANGLE_MAX);
            return QuantizeFloat(wrapped, ANGLE_MIN, ANGLE_MAX, bits);
        }

        public static bool QuantizedEqual(float a, float b, float min, float max, int bits)
        {
            return QuantizeFloat(a, min, max, bits) ==
                   QuantizeFloat(b, min, max, bits);
        }
    }
}
