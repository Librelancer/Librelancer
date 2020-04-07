// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using LibreLancer.ImageLib;

namespace LancerEdit
{
    public enum DDSFormat
    {
        Uncompressed = 11,
        DXT1 = CrnglueFormat.DXT1,
        DXT1a = CrnglueFormat.DXT1A, 
        DXT3 = CrnglueFormat.DXT3,
        DXT5 = CrnglueFormat.DXT5
    }
    public enum MipmapMethod
    {
        None = CrnglueMipmaps.NONE,
        Box = CrnglueMipmaps.BOX,
        Tent = CrnglueMipmaps.TENT,
        Lanczos4 = CrnglueMipmaps.LANCZOS4,
        Mitchell = CrnglueMipmaps.MITCHELL,
        Kaiser = CrnglueMipmaps.KAISER
    }
    public class TextureImport
    {
        static Generic.LoadResult ReadFile(string input, bool flip)
        {
            using (var file = File.OpenRead(input))
            {
                return Generic.BytesFromStream(file, flip);
            }
        }
        static byte[] TargaRGBA(byte[] data, int width, int height)
        {
            using (var stream = new MemoryStream())
            {
                var writer = new BinaryWriter(stream);
                writer.Write((byte)0); //idlength
                writer.Write((byte)0); //no color map
                writer.Write((byte)2); //uncompressed rgb
                writer.Write((short) 0); //no color map
                writer.Write((short)0); //no color map
                writer.Write((byte) 0); //no color map
                writer.Write((short)0); //zero origin
                writer.Write((short)0); //zero origin
                writer.Write((ushort)width);
                writer.Write((ushort)height);
                writer.Write((byte)32); //32 bpp RGBA
                writer.Write((byte)0); //descriptor
                writer.Write(data);
                return stream.ToArray();
            }
        }
        public static byte[] TGANoMipmap(string input, bool flip)
        {
            var raw = ReadFile(input, flip);
            return TargaRGBA(raw.Data, raw.Width, raw.Height);
        }
        public static List<LUtfNode> TGAMipmaps(string input, MipmapMethod mipm, bool flip)
        {
            var raw = ReadFile(input, flip);
            var mips = Crunch.GenerateMipmaps(raw.Data, raw.Width, raw.Height, (CrnglueMipmaps) mipm);
            var nodes = new List<LUtfNode>(mips.Count);
            for (int i = 0; i < mips.Count; i++)
            {
                var n = new LUtfNode {Name = "MIP" + i, Data = TargaRGBA(mips[i].Bytes, mips[i].Width, mips[i].Height)};
                nodes.Add(n);
            }
            return nodes;
        }
        public static byte[] CreateDDS(string input, DDSFormat format, MipmapMethod mipm, bool slow, bool flip)
        {
            var raw = ReadFile(input, flip);
            return Crunch.CompressDDS(raw.Data, raw.Width, raw.Height, (CrnglueFormat) format, (CrnglueMipmaps) mipm, slow);
        }
    }
}
