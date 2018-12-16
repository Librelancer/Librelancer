// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using TeximpNet.Compression;
using LibreLancer;
namespace LancerEdit
{
    public enum DDSFormat
    {
        Uncompressed = CompressionFormat.BGRA,
        DXT1 = CompressionFormat.DXT1,
        DXT1a = CompressionFormat.DXT1a,
        DXT3 = CompressionFormat.DXT3,
        DXT5 = CompressionFormat.DXT5
    }
    public enum MipmapMethod
    {
        None = -1,
        Box = TeximpNet.ImageFilter.Box,
        Bicubic = TeximpNet.ImageFilter.Bicubic,
        Bilinear = TeximpNet.ImageFilter.Bilinear,
        Bspline = TeximpNet.ImageFilter.Bspline,
        CatmullRom = TeximpNet.ImageFilter.CatmullRom,
        Lanczos3 = TeximpNet.ImageFilter.Lanczos3
    }
    public class TextureImport
    {
        static bool first = true;

        public static void LoadLibraries()
        {
            if (first)
            {
                if (Platform.RunningOS == OS.Linux)
                {
                    TeximpNet.Unmanaged.FreeImageLibrary.Instance.LoadLibrary("libfreeimage.so.3");
                    var mypath = Path.GetDirectoryName(typeof(TextureImport).Assembly.Location);
                    TeximpNet.Unmanaged.NvTextureToolsLibrary.Instance.LoadLibrary(Path.Combine(mypath, "libnvtt.so"));
                }
                else if (Platform.RunningOS == OS.Windows)
                {
                    TeximpNet.Unmanaged.FreeImageLibrary.Instance.LoadLibrary("FreeImage32.dll", "FreeImage64.dll");
                    TeximpNet.Unmanaged.NvTextureToolsLibrary.Instance.LoadLibrary("nvtt32.dll", "nvtt64.dll");
                }
                first = false;
            }
        }

        static List<TeximpNet.Surface> GenerateMipmapsRGBA(string input, MipmapMethod mipm, bool flip)
        {
            List<TeximpNet.Surface> mips = new List<TeximpNet.Surface>();
            var surface = TeximpNet.Surface.LoadFromFile(input);
            if (flip) surface.FlipVertically();
            surface.ConvertTo(TeximpNet.ImageConversion.To32Bits);
            surface.GenerateMipMaps(mips, (TeximpNet.ImageFilter)mipm);
            return mips;
        }

        public static byte[] TGANoMipmap(string input, bool flip)
        {
            LoadLibraries();
            using(var stream = new MemoryStream()) {
                using(var surface = TeximpNet.Surface.LoadFromFile(input)) {
                    if (flip) surface.FlipVertically();
                    surface.ConvertTo(TeximpNet.ImageConversion.To32Bits);
                    surface.SaveToStream(TeximpNet.ImageFormat.TARGA, stream);
                }
                return stream.ToArray();
            }
        }
        public static unsafe List<LUtfNode> TGAMipmaps(string input, MipmapMethod mipm, bool flip)
        {
            LoadLibraries();
            var nodes = new List<LUtfNode>();
            var mips = GenerateMipmapsRGBA(input, mipm, flip);
            for (int i = 0; i < mips.Count; i++) {
                using(var stream = new MemoryStream()) {
                    mips[i].SaveToStream(TeximpNet.ImageFormat.TARGA, stream);
                    var n = new LUtfNode() { Name = "MIP" + i, Data = stream.ToArray() };
                    nodes.Add(n);
                    mips[i].Dispose();
                }
            }
            return nodes;
        }
        public static byte[] CreateDDS(string input, DDSFormat format, MipmapMethod mipm, bool slow, bool flip)
        {
            LoadLibraries();
            using (var stream = new MemoryStream())
            {
                using (var compress = new Compressor())
                {
                    compress.Input.GenerateMipmaps = false;
                    List<TeximpNet.Surface> toDispose = null;
                    if (mipm == MipmapMethod.None)
                    {
                        using (var surface = TeximpNet.Surface.LoadFromFile(input))
                        {
                            if (flip) surface.FlipVertically();
                            compress.Input.SetData(surface);
                        }
                    } else {
                        var mips = GenerateMipmapsRGBA(input, mipm, flip);
                        compress.Input.SetTextureLayout(TextureType.Texture2D, mips[0].Width, mips[0].Height);

                        for (int i = 0; i < mips.Count; i++) {
                            compress.Input.SetMipmapData(mips[i], i);
                        }
                        toDispose = mips;
                    }
                    compress.Compression.Format = (CompressionFormat)format;
                    compress.Compression.Quality = slow ? CompressionQuality.Production : CompressionQuality.Normal;
                    compress.Compression.SetBGRAPixelFormat();
                    compress.Process(stream);
                    if(toDispose != null) {
                        foreach (var sfc in toDispose)
                            sfc.Dispose();
                    }
                }
                return stream.ToArray();
            }
        }
    }
}
