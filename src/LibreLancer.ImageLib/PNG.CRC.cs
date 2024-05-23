// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer.ImageLib
{
	public static partial class PNG
	{
		static uint[] crcTable;
		private static void BuildCrcTable()
		{
			crcTable = new uint[256];

			uint c, n, k;

			for (n = 0; n < 256; n++)
			{
				c = n;

				for (k = 0; k < 8; k++)
				{
					if ((c & 1) > 0)
					{
						c = 0xedb88320 ^ (c >> 1);
					}
					else
					{
						c = c >> 1;
					}
				}

				crcTable[n] = c;
			}
		}
		static uint Crc(Span<byte> bytesA, Span<byte> bytesB)
		{
			if (crcTable == null)
				BuildCrcTable();
			uint crc = 0xffffffff;
			for (int i = 0; i < bytesA.Length; i++)
			{
				crc = crcTable[(crc ^ bytesA[i]) & 0xff] ^ (crc >> 8);
			}
			for (int i = 0; i < bytesB.Length; i++)
			{
				crc = crcTable[(crc ^ bytesB[i]) & 0xff] ^ (crc >> 8);
			}
			return crc ^ 0xffffffff;
		}
	}
}
