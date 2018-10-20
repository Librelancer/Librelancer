// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
namespace LibreLancer.Media
{
	static class BinaryReaderExtensions
	{
		public static void Skip(this BinaryReader reader, int bytes)
		{
			reader.BaseStream.Seek(bytes, SeekOrigin.Current);
		}
	}
}

