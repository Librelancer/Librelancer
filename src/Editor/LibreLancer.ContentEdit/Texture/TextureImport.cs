// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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

        static byte[] GetEmbeddedDDS(Image lr, Stream input)
        {
            try
            {
                using var reader = new BinaryReader(input);
                if (reader.ReadUInt64() != 0xA1A0A0D474E5089) //Not PNG
                    return null;
                Span<byte> fourcc = stackalloc byte[4];
                byte[] ddszChunk;
                while (true)
                {
                    var len = reader.ReadInt32BE();
                    int ret = reader.Read(fourcc);
                    if (ret == 0)
                        return null;
                    if (fourcc[0] == 'd' &&
                        fourcc[1] == 'd' &&
                        fourcc[2] == 's' &&
                        fourcc[3] == 'z')
                    {
                        ddszChunk = reader.ReadBytes(len);
                        break;
                    }
                    else
                    {
                        reader.Skip(len + 4);
                    }
                }
                byte[] hash = SHA256.HashData(lr.Data);
                if (!hash.AsSpan().SequenceEqual(ddszChunk.AsSpan().Slice(0, hash.Length)))
                {
                    FLLog.Info("Texture Import", "ddsz chunk detected, but hash mismatched");
                    return null;
                }
                FLLog.Info("Texture Import", "Importing embedded dds from ddsz chunk");
                return ddszChunk.AsSpan().Slice(hash.Length).ToArray();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static EditResult<AnalyzedTexture> OpenFile(string input, RenderContext context)
        {
            try
            {
                using (var file = File.OpenRead(input))
                {
                    if (DDS.StreamIsDDS(file))
                    {
                        return (new AnalyzedTexture()
                        {
                            Type = TexLoadType.DDS,
                            Texture = (Texture2D)DDS.FromStream(context, file)
                        }).AsResult();
                    }
                }
                Image lr;
                using (var file = File.OpenRead(input))
                {
                    lr = Generic.ImageFromStream(file);
                }
                using (var file = File.OpenRead(input))
                {
                    byte[] embedded;
                    if ((embedded = GetEmbeddedDDS(lr, file)) != null)
                    {
                        return (new AnalyzedTexture()
                        {
                            Type = TexLoadType.DDS,
                            Texture = (Texture2D)DDS.FromStream(context, new MemoryStream(embedded))
                        }).AsResult();
                    }
                }

                EditMessage[] warning = Array.Empty<EditMessage>();
                if (lr.Width != lr.Height)
                {
                    warning = new[] { EditMessage.Warning($"Dimensions of {input} are not square") };
                }
                if (!MathHelper.IsPowerOfTwo(lr.Width) ||
                    !MathHelper.IsPowerOfTwo(lr.Height))
                    return EditResult<AnalyzedTexture>.Error($"Dimensions of {input} are not powers of two");
                var opaque = true;
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
                return new EditResult<AnalyzedTexture>(new AnalyzedTexture()
                {
                    Type = opaque ? TexLoadType.Opaque : TexLoadType.Alpha,
                    Texture = tex
                }, warning);
            }
            catch (Exception e)
            {
                return EditResult<AnalyzedTexture>.Error($"Could not load file {input}");
            }
        }
        static Image ReadFile(string input, bool flip)
        {
            using (var file = File.OpenRead(input))
            {
                return Generic.ImageFromStream(file, flip);
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
                    var raw =  Generic.ImageFromStream(ms, false);
                    byte[] embedded;
                    if ((embedded = GetEmbeddedDDS(raw, new MemoryStream(input.ToArray()))) != null)
                    {
                        return new LUtfNode() { Name = "MIPS", Data = embedded, Parent = parent };
                    }
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
