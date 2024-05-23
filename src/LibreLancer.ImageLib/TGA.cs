// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using LibreLancer.Graphics;

namespace LibreLancer.ImageLib
{
	public static class TGA
	{

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct TGAHeader
        {
            public byte Offset;
            public byte Indexed;
            public byte ImageType;
            public ushort PaletteStart;
            public ushort PaletteLength;
            public byte PaletteBits;
            public ushort XOrigin;
            public ushort YOrigin;
            public ushort Width;
            public ushort Height;
            public byte BitsPerPixel;
            public byte Inverted;
        }

        static int BytesPerPixel(int bpp)
        {
            switch (bpp) {
                case 8: return 1;
                case 15:
                case 16:
                    return 2;
                case 24:
                    return 3;
                case 32:
                    return 4;
                default:
                    throw new InvalidDataException();
            }
        }

        private const int NO_IMAGE_DATA = 0;
        private const int INDEXED = 1;
        private const int RGB = 2;
        private const int BW = 3;

        public static Texture2D TextureFromStream(RenderContext context, Stream stream, bool hasMipMaps = false,
            Texture2D target = null,
            int mipLevel = -1)
        {
            int channels = 0;
            if (target != null)
                channels = target.Format == SurfaceFormat.Bgra5551 ? 2 : 4;
            var image = ImageFromStream(stream, channels);
            if (target == null)
            {
                var tex = new Texture2D(context, image.Width, image.Height, hasMipMaps, image.Format);
                tex.SetData(image.Data);
                tex.WithAlpha = image.Alpha;
                return tex;
            }
            else
            {
                target.SetData(mipLevel, null, image.Data, 0, image.Data.Length);
                return null;
            }
        }

        public static Image ImageFromStream(Stream stream, int channels = 0)
        {
            var reader = new BinaryReader(stream);
            var header = reader.ReadStruct<TGAHeader>();
            //bool imageInverted = (1 - ((header.Inverted >> 5) & 1)) != 0;

            if (header.Indexed > 1)
            {
                FLLog.Error("TGA", $"Unsupported palette {header.Indexed}");
                return null;
            }

            bool rle;
            int imageType;

            if (header.ImageType >= 8)
            {
                imageType = header.ImageType - 8;
                rle = true;
            }
            else
            {
                imageType = header.ImageType;
                rle = false;
            }

            if (imageType <= NO_IMAGE_DATA || imageType >= BW)
            {
                FLLog.Error("TGA", $"Unsupported image type {header.ImageType}");
                return null;
            }

            if (header.Offset > 0)
                stream.Seek(header.Offset, SeekOrigin.Begin);

            int bytesPerPixel = BytesPerPixel(header.Indexed == 1 ? header.PaletteBits : header.BitsPerPixel);
            byte[] tgaData = new byte[header.Width * header.Height * bytesPerPixel];

            if (!rle && header.Indexed == 0)
            {
                if (reader.BaseStream.Read(tgaData) < tgaData.Length)
                {
                    throw new EndOfStreamException();
                }
            }
            else
            {
                byte[] colorMap = null;
                if (header.Indexed == 1)
                {
                    reader.BaseStream.Seek(header.PaletteStart, SeekOrigin.Current);
                    colorMap = reader.ReadBytes(bytesPerPixel * header.PaletteLength);
                }

                bool readNextPixel = false;
                int rleCount = 0;
                bool rleRepeating = false;
                var dataSpan = tgaData.AsSpan();

                for (int i = 0; i < header.Width * header.Height; i++)
                {
                    if (rle)
                    {
                        if (rleCount == 0)
                        {
                            byte rlePacket = reader.ReadByte();
                            rleCount = 1 + (rlePacket & 127);
                            rleRepeating = (rlePacket >> 7) != 0;
                            readNextPixel = true;
                        }
                        else if (!rleRepeating)
                        {
                            readNextPixel = true;
                        }
                    }
                    else
                    {
                        readNextPixel = true;
                    }

                    if (readNextPixel)
                    {
                        if (colorMap != null)
                        {
                            int paletteIndex = header.BitsPerPixel == 8 ? reader.ReadByte() : reader.ReadUInt16();
                            if (paletteIndex >= header.PaletteLength)
                                paletteIndex = 0;
                            paletteIndex *= bytesPerPixel;
                            colorMap.AsSpan().Slice(paletteIndex, bytesPerPixel)
                                .CopyTo(dataSpan.Slice(i * bytesPerPixel));
                        }
                        else
                        {
                            if (reader.BaseStream.Read(dataSpan.Slice(i * bytesPerPixel, bytesPerPixel)) <
                                bytesPerPixel)
                                throw new EndOfStreamException();
                        }

                        readNextPixel = false;
                    }
                    else
                    {
                        dataSpan.Slice((i - 1) * bytesPerPixel, bytesPerPixel)
                            .CopyTo(dataSpan.Slice(i * bytesPerPixel));
                    }
                    rleCount--;
                }
            }

            //Technically we should flip these, but Freelancer does not
            //Leave unflipped

            /*if (imageInverted)
            {

                for (int j = 0; j * 2 < header.Height; ++j)
                {
                    int index1 = j * header.Width * bytesPerPixel;
                    int index2 = (header.Height - 1 - j) * header.Width * bytesPerPixel;
                    for (int i = header.Width * bytesPerPixel; i > 0; --i)
                    {
                        (tgaData[index1], tgaData[index2]) = (tgaData[index2], tgaData[index1]);
                        ++index1;
                        ++index2;
                    }
                }
            }*/


            //Conversion of texture data - if necessary
            int targetBytesPerPixel = 0;
            if (channels != 0)
                targetBytesPerPixel = channels;
            else if (bytesPerPixel == 3)
                targetBytesPerPixel = 4;
            else
                targetBytesPerPixel = bytesPerPixel;

            byte[] targetData;
            if (targetBytesPerPixel == bytesPerPixel)
            {
                targetData = tgaData;
                if (bytesPerPixel == 2 && targetBytesPerPixel == 2)
                {
                    var src = MemoryMarshal.Cast<byte, ushort>(tgaData);
                    for (int i = 0; i < header.Width * header.Height; i++)
                    {
                        src[i] |= (ushort)32768; //16-bit TGA images shouldn't display with alpha, set to 1
                    }
                }
            }
            else
            {
                targetData = new byte[header.Width * header.Height * targetBytesPerPixel];
                if (bytesPerPixel == 2 && targetBytesPerPixel == 4)
                {
                    var src = MemoryMarshal.Cast<byte, ushort>(tgaData);
                    var dst = MemoryMarshal.Cast<byte, uint>(targetData);
                    for (int i = 0; i < header.Width * header.Height; i++)
                    {
                        var val = src[i];
                        var r = (uint) ((val & 0x7C00) >> 10);
                        r = (r << 3) | (r >> 2);
                        var g = (uint) ((val & 0x03E0) >> 5);
                        g = (g << 3) | (g >> 2);
                        var b = (uint) (val & 0x001F);
                        b = (b << 3) | (b >> 2);
                        dst[i] = 0xFF000000 | (b << 16) | (g << 8) | r;
                    }
                }
                else if (bytesPerPixel == 3 && targetBytesPerPixel == 4)
                {
                    var dst = MemoryMarshal.Cast<byte, uint>(targetData);
                    for (int i = 0; i < header.Width * header.Height; i++)
                    {
                        var j = i * 3;
                        var r = (uint) tgaData[j];
                        var g = (uint) tgaData[j + 1];
                        var b = (uint) tgaData[j + 2];
                        dst[i] = 0xFF000000 | (b << 16) | (g << 8) | r;
                    }
                }
                else if ((bytesPerPixel == 3 || bytesPerPixel == 4) && targetBytesPerPixel == 2)
                {
                    var dst = MemoryMarshal.Cast<byte, ushort>(targetData);
                    for (int i = 0; i < header.Width * header.Height; i++)
                    {
                        var j = i * bytesPerPixel;
                        var r = (uint) tgaData[j];
                        var g = (uint) tgaData[j + 1];
                        var b = (uint) tgaData[j + 2];

                        dst[i] = (ushort)(32768 |
                                          ((r >> 3) << 10) |
                                          ((g >> 3) << 5) |
                                          ((b >> 3)));
                    }
                }
                else
                {
                    throw new NotImplementedException($"Convert from {bytesPerPixel} bytes to {targetBytesPerPixel}");
                }
            }

            return new Image()
            {
                Alpha = bytesPerPixel == 4 && imageType != 1, Data = targetData,
                Format = bytesPerPixel == 2 ? SurfaceFormat.Bgra5551 : SurfaceFormat.Bgra8,
                Width = header.Width, Height = header.Height
            };
        }
    }
}
