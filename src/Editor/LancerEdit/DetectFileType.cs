// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
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
        SaveGame,
        Other,
        Error
	}
	class DetectFileType
    {
        private static readonly byte[] UtfPattern = { 0x55, 0x54, 0x46, 0x20, 0x01, 0x01, 0x00, 0x00 };
        private static readonly byte[] XUtfPattern = { 0x58, 0x55, 0x54, 0x46, 0x01 };
        private static readonly byte[] ThnPattern = { 0x1B, 0x4C, 0x75, 0x61, 0x32 };
        private static readonly byte[] SavePattern = { 0x46, 0x4C, 0x53, 0x31 };

        static bool PatternCheck(byte[] pat, Span<byte> src)
        {
            if (src.Length < pat.Length) return false;
            for(int i = 0; i < pat.Length; i++)
                if (pat[i] != src[i])
                    return false;
            return true;
        }

        static bool AnyByte(Span<byte> b, Func<byte, bool> pred)
        {
            for(int i = 0 ; i < b.Length; i++)
                if (pred(b[i]))
                    return true;
            return false;
        }

		public static FileType Detect(string filename)
		{
			using (var stream = File.OpenRead(filename))
            {
                Span<byte> bytes = stackalloc byte[128];
                bytes = bytes.Slice(0, stream.Read(bytes));
                if (bytes.Length == 0)
                    return FileType.Error;
                if (PatternCheck(UtfPattern, bytes) || PatternCheck(XUtfPattern, bytes))
                    return FileType.Utf;
                if (PatternCheck(ThnPattern, bytes))
                    return FileType.Thn;
                if (PatternCheck(SavePattern, bytes))
                    return FileType.SaveGame;
                if (!AnyByte(bytes, b => b >= 128))
                {
                    // Lua code will usually contain a # { } or = somewhere in the first few bytes.
                    var text = Encoding.ASCII.GetString(bytes);
                    if (text.Any(c => c == '#' || c == '{' || c == '}' || c == '=') &&
                        (filename.EndsWith(".lua", StringComparison.OrdinalIgnoreCase) ||
                        filename.EndsWith(".thn", StringComparison.OrdinalIgnoreCase)))
                    {
                        return FileType.Lua;
                    }
                }
			}
            if (Blender.FileIsBlender(filename))
                return FileType.Blender;
            return FileType.Other;
        }
	}
}
