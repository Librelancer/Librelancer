/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2018
 * the Initial Developer. All Rights Reserved.
 */
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
		static uint Crc(byte[] bytesA, byte[] bytesB)
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
