// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Text;
using System.IO;
namespace LibreLancer
{
	public static class BinaryReaderExtensions
	{
		static byte[] tagBuf = new byte[4];

		public static string ReadTag(this BinaryReader reader)
		{
			reader.BaseStream.Read (tagBuf, 0, 4);
			return Encoding.ASCII.GetString (tagBuf);
		}

		public static uint ReadUInt24(this BinaryReader reader)
		{
			return (uint)reader.ReadByte() + ((uint)reader.ReadByte() << 8) + ((uint)reader.ReadByte() << 16);
		}
	}
}

