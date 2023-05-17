// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.IO;
using System.Linq;
using System.Text;
using LibreLancer.ContentEdit;

namespace LancerEdit
{
	enum FileType
	{
		Utf,
        Thn,
        Lua,
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
                var str = Encoding.ASCII.GetString(header);
				if (str == "UTF " && reader.ReadInt32() == 257) return FileType.Utf;
                if (str == "XUTF" && reader.ReadByte() == 1) return FileType.Utf;
                if (str.EndsWith("Lua") && header[0] == 0x1b && reader.ReadByte() == 0x32) return FileType.Thn;
                
                // Read ahead and check to see if the file is likely ASCII
                var block = reader.ReadBytes(60); 
                if (!block.Any(b => b >= 128))
                {
                    // Lua code will usually contain a # { } or = somewhere in the first few bytes.
                    var text = Encoding.ASCII.GetString(block);
                    if (text.Any(c => c == '#' || c == '{' || c == '}' || c == '=') && filename.EndsWith(".lua")) return FileType.Lua;
                }
			}
            if (Blender.FileIsBlender(filename))
                return FileType.Blender;
            return FileType.Other;
        }
	}
}
