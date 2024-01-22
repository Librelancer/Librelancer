// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using LibreLancer.Graphics;
using LibreLancer.ImageLib;

namespace LibreLancer.ContentEdit
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

    public enum TexLoadType
    {
        DDS,
        Opaque,
        Alpha,
        ErrorNonSquare,
        ErrorLoad,
        ErrorNonPowerOfTwo
    }
    public class AnalyzedTexture
    {
        public TexLoadType Type;
        public Texture2D Texture;
    }

    public class TextureImport
    {
        public static string LoadErrorString(TexLoadType type, string filename)
        {
            switch (type)
            {
                case TexLoadType.ErrorLoad: return $"Could not load file {filename}";
                case TexLoadType.ErrorNonSquare: return $"Dimensions of {filename} are not square";
                case TexLoadType.ErrorNonPowerOfTwo: return $"Dimensions of {filename} are not powers of two";
                default: throw new InvalidOperationException();
            }
        }
        public static AnalyzedTexture OpenFile(string input, RenderContext context)
        {
            try
            {
                using (var file = File.OpenRead(input))
                {
                    if (DDS.StreamIsDDS(file))
                    {
                        return new AnalyzedTexture()
                        {
                            Type = TexLoadType.DDS,
                            Texture = (Texture2D)DDS.FromStream(context, file)
                        };
                    }
                }
                Generic.LoadResult lr;
                using (var file = File.OpenRead(input))
                {
                    lr = Generic.BytesFromStream(file);
                }
                if (lr.Width != lr.Height)
                    return new AnalyzedTexture() {Type = TexLoadType.ErrorNonSquare};
               if(!MathHelper.IsPowerOfTwo(lr.Width) ||
                  !MathHelper.IsPowerOfTwo(lr.Height))
                   return new AnalyzedTexture() { Type = TexLoadType.ErrorNonPowerOfTwo};
               bool opaque = true;
               //Swap channels + check alpha
               for (int i = 0; i < lr.Data.Length; i += 4)
               {
                   var R = lr.Data[i];
                   var B = lr.Data[i + 2];
                   var A = lr.Data[i + 3];
                   lr.Data[i + 2] = R;
                   lr.Data[i] = B;
                   if (A != 255)
                   {
                       opaque = false;
                   }
               }
               var tex = new Texture2D(context, lr.Width, lr.Height);
               tex.SetData(lr.Data);
               return new AnalyzedTexture()
               {
                   Type = opaque ? TexLoadType.Opaque : TexLoadType.Alpha,
                   Texture = tex
               };
            }
            catch (Exception e)
            {
                return new AnalyzedTexture() {Type = TexLoadType.ErrorLoad};
            }
        }
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
                for (int i = 0; i < data.Length; i += 4)
                {
                    //BGRA storage
                    writer.Write(data[i + 2]);
                    writer.Write(data[i + 1]);
                    writer.Write(data[i]);
                    writer.Write(data[i + 3]);
                }
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

        public static LUtfNode ImportAsMIPSNode(ReadOnlySpan<byte> input, LUtfNode parent)
        {
            var data = new byte[input.Length];
            input.CopyTo(data);
            using (var ms = new MemoryStream(data))
            {
                if (DDS.StreamIsDDS(ms))
                    return new LUtfNode() {Name = "MIPS", Data = data, Parent = parent};
                else
                {
                    var raw =  Generic.BytesFromStream(ms, false);
                    data =  Crunch.CompressDDS(raw.Data, raw.Width, raw.Height, CrnglueFormat.DXT5, CrnglueMipmaps.LANCZOS4, false);
                    return new LUtfNode() {Name = "MIPS", Data = data, Parent = parent };
                }
            }
        }

        public static byte[] CreateDDS(string input, DDSFormat format, MipmapMethod mipm, bool slow, bool flip)
        {
            var raw = ReadFile(input, flip);
            return Crunch.CompressDDS(raw.Data, raw.Width, raw.Height, (CrnglueFormat) format, (CrnglueMipmaps) mipm, slow);
        }
    }
}
