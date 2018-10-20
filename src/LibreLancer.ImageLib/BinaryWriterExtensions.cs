// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
namespace LibreLancer.ImageLib
{
	static class BinaryWriterExtensions
	{
		public static void WriteInt32BE(this BinaryWriter writer, int val)
		{
			if (BitConverter.IsLittleEndian)
			{
				var bytes = BitConverter.GetBytes(val);
				for (int i = 3; i >= 0; i--)
					writer.Write(bytes[i]);
			}
			else
				writer.Write(val);
		}
	}
}
