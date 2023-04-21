// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.IO;
using System.IO.Compression;
using System.Text;

namespace ShaderProcessor;

public static class Compress
{
    public static byte[] GetBytes(string s, bool brotli)
    {
        using (var strm = new MemoryStream())
        {
            if (brotli)
                using (var comp = new BrotliStream(strm, CompressionLevel.Optimal, true))
                {
                    var txt = Encoding.UTF8.GetBytes(s);
                    comp.Write(txt, 0, txt.Length);
                }
            else
                using (var comp = new DeflateStream(strm, CompressionLevel.Optimal, true))
                {
                    var txt = Encoding.UTF8.GetBytes(s);
                    comp.Write(txt, 0, txt.Length);
                }

            return strm.ToArray();
        }
    }
}