// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Ini;

public static class IniFile
{
    public static IEnumerable<Section> ParseFile(string path, Stream stream, bool preparse = true, bool allowmaps = false, IniStringPool? stringPool = null)
    {
        if (string.IsNullOrEmpty(path)) path = "[Memory]";
        IIniParser parser = new BinaryIniParser();
        if (!parser.CanParse(stream))
        {
            parser = new LancerTextIniParser();
        }
        return parser.ParseIniFile(path, stream, preparse, allowmaps, stringPool);
    }

    public static IEnumerable<Section> ParseFile(string path, FileSystem? vfs, bool allowmaps = false, IniStringPool? stringPool = null)
    {
        if (!path.EndsWith(".ini", StringComparison.OrdinalIgnoreCase))
        {
            path += ".ini";
        }

        using var stream = new MemoryStream();

        //Don't wait on I/O for yield return
        if (vfs == null)
        {
            using Stream file = File.OpenRead(path);

            file.CopyTo(stream);
        }
        else
        {
            using Stream file = vfs.Open(path);

            file.CopyTo(stream);

        }

        stream.Seek(0, SeekOrigin.Begin);
        foreach (var s in ParseFile(path, stream, true, allowmaps, stringPool))
        {
            yield return s;
        }
    }
}
