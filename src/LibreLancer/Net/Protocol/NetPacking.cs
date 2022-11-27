// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

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
