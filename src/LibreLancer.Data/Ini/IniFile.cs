// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LibreLancer.Data;
using LibreLancer.Data.IO;

namespace LibreLancer.Ini
{
	public abstract partial class IniFile
	{
        private const string Bini = "BINI";

        private static string DetermineFileType(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            byte[] buffer = new byte[4];
            stream.Read(buffer, 0, 4);
            var fileType = Encoding.ASCII.GetString(buffer);
            stream.Seek(0, SeekOrigin.Begin);
            return fileType;
        }

        protected IEnumerable<Section> ParseFile(string path, Stream stream, bool preparse = true, bool allowmaps = false)
        {
            if (string.IsNullOrEmpty(path)) path = "[Memory]";
            
            var fileType = DetermineFileType(stream);

            IIniParser parser = null;
            if (fileType == Bini) 
            {
                // Binary Ini
                parser = new BinaryIniParser();
            }
            else 
            {
                // Text Ini
                parser = new TextIniParser();
            }
            return parser.ParseIniFile(path, stream, preparse, allowmaps);
        }

		protected IEnumerable<Section> ParseFile(string path, FileSystem vfs, bool allowmaps = false)
		{
			if (path == null) throw new ArgumentNullException(nameof(path));
			if (!path.EndsWith(".ini", StringComparison.OrdinalIgnoreCase))
				path = path + ".ini";
            using (var stream = new MemoryStream())
            {
                //Don't wait on I/O for yield return
                if (vfs == null)
                {
                    using (Stream file = File.OpenRead(path))
                    {
                        file.CopyTo(stream);
                    }
                }
                else
                {
                    using (Stream file = vfs.Open(path))
                    {
                        file.CopyTo(stream);
                    }
                }

                foreach (var s in ParseFile(path, stream, true, allowmaps)) yield return s;
            }
		}
	}
}
