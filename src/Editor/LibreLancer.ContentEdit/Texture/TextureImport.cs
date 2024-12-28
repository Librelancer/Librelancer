// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
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
        DXT5 = CrnglueFormat.DXT5,
        RGTC2 = CrnglueFormat.RGTC2,
        MetallicRGTC1 = CrnglueFormat.MetallicRGTC1,
        RoughnessRGTC1 = CrnglueFormat.RoughnessRGTC1,
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
        Alpha
    }

    public class AnalyzedTexture
    {
        public TexLoadType Type;
        public Texture2D Texture;
        public bool OneBitAlpha = false;
        public byte[] Source;
    }

    public class TextureImport
    {
        static byte[] GetEmbeddedDDS(Image lr, Stream input)
        {
            try
            {
                using var reader = new BinaryReader(input);
                if (reader.ReadUInt64() != 0xA1A0A0D474E5089) //Not PNG
                    return null;
                Span<byte> fourcc = stackalloc byte[4];
                byte[] ddszChunk = null;
                while ((reader.BaseStream.Position + 4) < reader.BaseStream.Length)
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
                if (ddszChunk == null)
                    return null;
                byte[] hash = SHA256.HashData(lr.Data);
                if (!hash.AsSpan().SequenceEqual(ddszChunk.AsSpan().Slice(0, hash.Length)))
                {
                    FLLog.Info("Texture Import", "ddsz chunk detected, but hash mismatched");
                    return null;
                }
                FLLog.Info("Texture Import", "Importing embedded dds from ddsz chunk");
                using var decomp = new ZstdSharp.Decompressor();
                return decomp.Unwrap(ddszChunk.AsSpan().Slice(hash.Length)).ToArray();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static EditResult<AnalyzedTexture> OpenBuffer(byte[] input, RenderContext context)
        {
            try
            {
                using (var file = new MemoryStream(input))
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
                using (var file = new MemoryStream(input))
                {
                    lr = Generic.ImageFromStream(file);
                }
                using (var file = new MemoryStream(input))
                {
                    byte[] embedded;
                    if ((embedded = GetEmbeddedDDS(lr, file)) != null)
                    {
                        return (new AnalyzedTexture()
                        {
                            Type = TexLoadType.DDS,
                            Texture = context == null ? null : (Texture2D)DDS.FromStream(context, new MemoryStream(embedded)),
                            Source = input
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
                var pixels = Bgra8.BufferFromBytes(lr.Data);
                //Swap channels + check alpha
                bool oneBitAlpha = true;
                for (int i = 0; i < pixels.Length; i++)
                {
                    if (pixels[i].A != 255)
                    {
                        opaque = false;
                    }
                    if (pixels[i].A != 255 && pixels[i].A != 0)
                    {
                        oneBitAlpha = false;
                    }
                    if (!opaque && !oneBitAlpha)
                        break;
                }
                if (opaque)
                    oneBitAlpha = false;
                Texture2D tex = null;
                if (context != null)
                {
                    tex = new Texture2D(context, lr.Width, lr.Height);
                    tex.SetData(lr.Data);
                }
                return new EditResult<AnalyzedTexture>(new AnalyzedTexture()
                {
                    Type = opaque ? TexLoadType.Opaque : TexLoadType.Alpha,
                    Texture = tex, OneBitAlpha = oneBitAlpha, Source = input
                }, warning);
            }
            catch (Exception e)
            {
                return EditResult<AnalyzedTexture>.Error($"Could not load file {input}");
            }
        }
        static Image ReadBuffer(byte[] input, bool flip)
        {
            using (var file = new MemoryStream(input))
            {
                return Generic.ImageFromStream(file, flip);
            }
        }

        static (bool Alpha, bool Palette, int PaletteCount) SmallestEncoding(ReadOnlySpan<Bgra8> pixels, Span<Bgra8> palette)
        {
            bool alpha = false;
            int px = 0;
            for (int i = 0; i < pixels.Length; i++) {
                if (pixels[i].A != 255) {
                    alpha = true;
                }
                if (px != -1 && !palette.Slice(0, px).Contains(pixels[i])) {
                    if (px == 255) {
                        px = -1;
                    } else {
                        palette[px++] = pixels[i];
                    }
                }
                if (px == -1 && !alpha) {
                    break;
                }
            }
            int bytesPerPixel = alpha ? 4 : 3;
            if (px != -1 && ((bytesPerPixel * px) + (pixels.Length)) < (bytesPerPixel * pixels.Length)) {
                return (alpha, true, px);
            } else {
                return (alpha, false, -1);
            }
        }

        static byte[] NoPalette(ReadOnlySpan<Bgra8> pixels, bool alpha, int width, int height)
        {
            FLLog.Debug("Targa", $"Generating {(alpha ? 32 : 24)}-bit RGB image");
            using var stream = new MemoryStream();
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
            writer.Write((byte)(alpha ? 32 : 24)); //32 bpp BGRA
            writer.Write((byte)0); //descriptor
            for (int i = 0; i < pixels.Length; i++)
            {
                writer.Write(pixels[i].B);
                writer.Write(pixels[i].G);
                writer.Write(pixels[i].R);
                if(alpha) writer.Write(pixels[i].A);
            }
            return stream.ToArray();
        }

        static byte[] TargaRGBA(byte[] data, int width, int height)
        {
            var pixels = Bgra8.BufferFromBytes(data);
            Span<Bgra8> palette = stackalloc Bgra8[256];
            var (alpha, usePalette, paletteCount) = SmallestEncoding(pixels, palette);
            if (!usePalette)
                return NoPalette(pixels, alpha, width, height);
            FLLog.Debug("Targa", $"Generating {(alpha ? 32 : 24)}-bit indexed image ({paletteCount})");
            using var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);
            writer.Write((byte)0); //idlength
            writer.Write((byte)1); //indexed
            writer.Write((byte)1); //indexed
            writer.Write((short)0); //offset 0
            writer.Write((short)paletteCount); //palette length
            writer.Write((byte)(alpha ? 32 : 24)); //palette bits
            writer.Write((short)0); //zero origin
            writer.Write((short)0); //zero origin
            writer.Write((ushort)width);
            writer.Write((ushort)height);
            writer.Write((byte)8); //8 bit palette index
            writer.Write((byte)0); //descriptor
            for (int i = 0; i < paletteCount; i++)
            {
                writer.Write(palette[i].B);
                writer.Write(palette[i].G);
                writer.Write(palette[i].R);
                if(alpha) writer.Write(palette[i].A);
            }
            for (int i = 0; i < pixels.Length; i++)
            {
                writer.Write((byte)(palette.IndexOf(pixels[i])));
            }
            return stream.ToArray();
        }


        public static byte[] TGANoMipmap(byte[] input, bool flip)
        {
            var raw = ReadBuffer(input, flip);
            return TargaRGBA(raw.Data, raw.Width, raw.Height);
        }
        public static List<LUtfNode> TGAMipmaps(byte[] input, MipmapMethod mipm, bool flip)
        {
            var raw = ReadBuffer(input, flip);
            var mips = Crunch.GenerateMipmaps(Bgra8.BufferFromBytes(raw.Data), raw.Width, raw.Height, (CrnglueMipmaps) mipm);
            //Limit mips from MIP0 to MIP9 or we generate invalid txm nodes
            var nodes = new List<LUtfNode>(mips.Count > 9 ? 9 : mips.Count);
            for (int i = 0; i < mips.Count && i <= 9; i++)
            {
                var n = new LUtfNode {Name = "MIP" + i, Data = TargaRGBA(mips[i].Bytes, mips[i].Width, mips[i].Height)};
                nodes.Add(n);
            }
            return nodes;
        }

        public static LUtfNode ImportAsMIPSNode(ReadOnlySpan<byte> input, LUtfNode parent, DDSFormat format = DDSFormat.DXT5)
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
                    data =  Crunch.CompressDDS(Bgra8.BufferFromBytes(raw.Data), raw.Width, raw.Height,
                        (CrnglueFormat)format, CrnglueMipmaps.LANCZOS4, false);
                    return new LUtfNode() {Name = "MIPS", Data = data, Parent = parent };
                }
            }
        }

        public static byte[] CreateDDS(ReadOnlySpan<Bgra8> input, int width, int height, DDSFormat format, MipmapMethod mipm, bool slow)
        {
            return Crunch.CompressDDS(input, width, height, (CrnglueFormat) format, (CrnglueMipmaps) mipm, slow);
        }

        public static byte[] CreateDDS(byte[] input, DDSFormat format, MipmapMethod mipm, bool slow, bool flip)
        {
            var raw = ReadBuffer(input, flip);
            return Crunch.CompressDDS(Bgra8.BufferFromBytes(raw.Data), raw.Width, raw.Height, (CrnglueFormat) format, (CrnglueMipmaps) mipm, slow);
        }
    }
}
