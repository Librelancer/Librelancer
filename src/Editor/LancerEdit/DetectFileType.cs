// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
namespace LancerEdit
{
	enum FileType
	{
		Utf,
		Ini
	}
	class DetectFileType
	{
		public static FileType Detect(string filename)
		{
			using (var reader = new BinaryReader(File.OpenRead(filename)))
			{
				var str = System.Text.Encoding.ASCII.GetString(reader.ReadBytes(4));
				if (str == "UTF ") if (reader.ReadInt32() == 257) return FileType.Utf;
				return FileType.Ini;
			}
		}
	}
}
