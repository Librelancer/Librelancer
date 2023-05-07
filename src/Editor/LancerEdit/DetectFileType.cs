// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using LibreLancer.ContentEdit;

namespace LancerEdit
{
	enum FileType
	{
		Utf,
        Thn,
		Blender,
        Other,
	}
	class DetectFileType
	{
		public static FileType Detect(string filename)
		{
			using (var reader = new BinaryReader(File.OpenRead(filename)))
			{
                var header = reader.ReadBytes(4);
                var str = System.Text.Encoding.ASCII.GetString(header);
				if (str == "UTF " && reader.ReadInt32() == 257) return FileType.Utf;
                if (str == "XUTF" && reader.ReadByte() == 1) return FileType.Utf;
                if (str.EndsWith("Lua") && header[0] == 0x1b && reader.ReadByte() == 0x32) return FileType.Thn;
			}
            if (Blender.FileIsBlender(filename))
                return FileType.Blender;
            return FileType.Other;
        }
	}
}
