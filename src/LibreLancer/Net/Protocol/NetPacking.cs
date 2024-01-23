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
        public const float UNIT_MIN = -0.70710677f;
        public const float UNIT_MAX = 0.70710677f;
        public const float ANGLE_MIN = (float)(-2 * Math.PI);
        public const float ANGLE_MAX = (float)(2 * Math.PI);

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

        static float WrapMinMax(float x, float min, float max)
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

        public static bool ApproxEqual(float a, float b, float epsilon = 0.001f) =>
            Math.Abs(a - b) < epsilon;

        public static bool ApproxEqual(Vector3 a, Vector3 b) =>
            (a - b).Length() < 0.0001f;

        public static bool QuantizedEqual(float a, float b, float min, float max, int bits)
        {
            return NetPacking.QuantizeFloat(a, min, max, bits) ==
                   NetPacking.QuantizeFloat(b, min, max, bits);
        }

        public static bool QuantizedEqual(Vector3 a, Vector3 b, float min, float max, int bits)
        {
            var aX = NetPacking.QuantizeFloat(a.X, min, max, bits);
            var aY = NetPacking.QuantizeFloat(a.Y, min, max, bits);
            var aZ = NetPacking.QuantizeFloat(a.Z, min, max, bits);

            var bX = NetPacking.QuantizeFloat(b.X, min, max, bits);
            var bY = NetPacking.QuantizeFloat(b.Y, min, max, bits);
            var bZ = NetPacking.QuantizeFloat(b.Z, min, max, bits);

            return aX == bX &&
                   aY == bY &&
                   aZ == bZ;
        }

        public static readonly string[] DefaultHpidData = new[]
        {
            "internal",
            "HpCM01",
            "HpCargo01",
            "HpCargo02",
            "HpContrail01",
            "HpContrail02",
            "HpDockLight01",
            "HpDockLight02",
            "HpDockLight03",
            "HpHeadlight",
            "HpMine01",
            "HpRunningLight01",
            "HpRunningLight02",
            "HpRunningLight03",
            "HpRunningLight04",
            "HpRunningLight05",
            "HpRunningLight06",
            "HpRunningLight07",
            "HpRunningLight10",
            "HpRunningLight11",
            "HpRunningLight12",
            "HpRunningLight13",
            "HpShield01",
            "HpThruster01",
            "HpTurret01",
            "HpTurret02",
            "HpTurret03",
            "HpTurret04",
            "HpTurret05",
            "HpTurret06",
            "HpTurret07",
            "HpTurret08",
            "HpTurret09",
            "HpTurret_U1_01",
            "HpTurret_U1_02",
            "HpTurret_U1_03",
            "HpTurret_U1_04",
            "HpTurret_U1_05",
            "HpTurret_U3_01",
            "HpWeapon01",
            "HpWeapon02",
            "HpWeapon03",
            "HpWeapon04",
        };
    }
}
