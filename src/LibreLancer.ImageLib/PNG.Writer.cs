// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace LibreLancer.ImageLib
{

    public record PNGAncillaryChunk(string FourCC, byte[] Data);

    class ZlibCompress : IDisposable
    {
        private DeflateStream deflate;
        private Stream outputStream;

        private const int ADLER_MOD = 65521;
        private uint checksum_a = 1;
        private uint checksum_b = 0;

        public ZlibCompress(Stream outputStream)
        {
            this.outputStream = outputStream;
            //write zlib header
            outputStream.WriteByte(0x78);
            outputStream.WriteByte(0xDA);
            deflate = new DeflateStream(outputStream, CompressionLevel.SmallestSize, true);
        }

        public void WriteByte(byte b)
        {
            deflate.WriteByte(b);
            checksum_a = (checksum_a + b) % ADLER_MOD;
            checksum_b = (checksum_b + checksum_a) % ADLER_MOD;
        }

        public void Write(Span<byte> buffer)
        {
            deflate.Write(buffer);
            for (int i = 0; i < buffer.Length; i++)
            {
                checksum_a = (checksum_a + buffer[i]) % ADLER_MOD;
                checksum_b = (checksum_b + checksum_a) % ADLER_MOD;
            }
        }
        public void Dispose()
        {
            deflate.Dispose();
            //
            var adler32 = (checksum_b << 16) | checksum_a;
            var bytes = BitConverter.GetBytes(adler32);
            if (BitConverter.IsLittleEndian)
            {
                for (int i = 3; i >= 0; i--)
                    outputStream.WriteByte(bytes[i]);
            }
            else
            {
                for (int i = 0; i < 4; i++)
                    outputStream.WriteByte(bytes[i]);
            }
        }
    }
	public static partial class PNG
	{
		const ulong PNG_SIGNATURE = 0xA1A0A0D474E5089;
		enum ColorType
		{
			Grayscale = 0,
			Rgb = 2,
			Palette = 3,
			GrayscaleAlpha = 4,
			Rgba = 6
		}

		static readonly byte[] IEND = {
			0x00, 0x00, 0x00, 0x00,
			0x49, 0x45, 0x4E, 0x44,
			0xAE, 0x42, 0x60, 0x82
		};

        static (ColorType, int bytes) Analyze(ReadOnlySpan<Bgra8> data)
        {
            bool alpha = false;
            bool gray = true;
            for (int i = 0; i < data.Length; i++) {
                if (data[i].R != data[i].G ||
                    data[i].G != data[i].B)
                    gray = false;
                if (data[i].A != 255)
                    alpha = true;
                if (alpha && !gray)
                    break;
            }

            if (gray && !alpha) return (ColorType.Grayscale, 1);
            if (gray) return (ColorType.GrayscaleAlpha, 2);
            if (!alpha) return (ColorType.Rgb, 3);
            return (ColorType.Rgba, 4);
        }

        static void CopyLine(Span<byte> source, Span<byte> dest, ColorType type, int width)
        {
            switch (type)
            {
                case ColorType.Grayscale:
                    for (int i = 0; i < width; i++)
                    {
                        dest[i] = source[i * 4];
                    }
                    break;
                case ColorType.GrayscaleAlpha:
                    for (int i = 0; i < width; i++)
                    {
                        dest[i * 2] = source[i * 4];
                        dest[i * 2 + 1] = source[i * 4 + 3];
                    }
                    break;
                case ColorType.Rgb:
                    for (int i = 0; i < width; i++)
                    {
                        int s = i * 4;
                        int d = i * 3;
                        dest[d] = source[s];
                        dest[d + 1] = source[s + 1];
                        dest[d + 2] = source[s + 2];
                    }
                    break;
                default:
                    source.Slice(0, width * 4).CopyTo(dest);
                    break;
            }
        }

        enum FilterType : byte
        {
            None = 0,
            Sub = 1,
            Up =  2,
            Average = 3,
            Paeth = 4
        }

		public static void Save(Stream output, int width, int height, ReadOnlySpan<Bgra8> bgraData, bool flip, params PNGAncillaryChunk[] ancillaryChunks)
        {
            var buffer = new Bgra8[bgraData.Length];
            if (flip) {
                for (int i = 0; i < height; i++)
                {
                    var dst = buffer.AsSpan().Slice(i * width, width);
                    var src = bgraData.Slice((height - 1 - i) * width, width);
                    src.CopyTo(dst);
                }
            }
            else
            {
                bgraData.CopyTo(buffer.AsSpan());
            }
            Bgra8.ConvertFromRgba(buffer); //Reverse the swap, make it Rgba
            var (colorType, bpp) = Analyze(buffer);
            var data = MemoryMarshal.Cast<Bgra8, byte>(buffer);

			using (var writer = new BinaryWriter(output))
			{
				writer.Write(PNG_SIGNATURE);
				using(var ihdr = new Chunk("IHDR"))
				{
					ihdr.Writer.WriteInt32BE(width);
                    ihdr.Writer.WriteInt32BE(height);
                    ihdr.Writer.Write((byte)8);
                    ihdr.Writer.Write((byte)colorType);
                    ihdr.Writer.Write((byte)0); //Compression method
                    ihdr.Writer.Write((byte)0); //Filter method
                    ihdr.Writer.Write((byte)0); //Interlacing
                    ihdr.WriteTo(writer);
				}
				using(var idat = new Chunk("IDAT"))
				{
					//zlib header
					//deflate compression
                    Span<byte> prev = stackalloc byte[width * bpp];
					Span<byte> buf = stackalloc byte[width * bpp];
                    byte[] filterCombos = new byte[width * bpp * 4];
                    using (var compress = new ZlibCompress(idat.Writer.BaseStream))
                    {
                        //First line
                        CopyLine(data.Slice(0, width * 4), prev, colorType, width);
                        compress.WriteByte((byte) 0);
                        for (int x = 0; x < prev.Length; x++)
                        {
                            compress.WriteByte(prev[x]);
                        }
                        //Filtered lines
                        for (int y = 1; y < height; y++)
                        {
                            CopyLine(data.Slice(y * width * 4), buf, colorType, width);
                            var filtered = ApplyFilters(prev, buf, width, bpp, filterCombos, out var filter);
                            compress.WriteByte((byte) filter); //paeth filter
                            compress.Write(filtered);
                            Span<byte> temp = prev;
                            prev = buf;
                            buf = temp;
                        }
                    }
                    idat.WriteTo(writer);
                }
                if (ancillaryChunks != null)
                {
                    foreach (var ac in ancillaryChunks)
                    {
                        if (ac.FourCC is not { Length: 4 })
                            throw new Exception("Invalid ancillary FourCC, length must == 4");
                        using var chk = new Chunk(ac.FourCC);
                        chk.Writer.Write(ac.Data);
                        chk.WriteTo(writer);
                    }
                }
				writer.Write(IEND);
			}
		}

        struct Chunk: IDisposable
        {
            public BinaryWriter Writer;
            private MemoryStream stream;
            private byte id0;
            private byte id1;
            private byte id2;
            private byte id3;

            public Chunk(string id)
            {
                id0 = (byte)id[0];
                id1 = (byte)id[1];
                id2 = (byte)id[2];
                id3  = (byte)id[3];
                stream = new MemoryStream();
                Writer = new BinaryWriter(stream);
            }

            public void WriteTo(BinaryWriter png)
            {
                var data = stream.ToArray();
                Span<byte> idbytes = stackalloc byte[4];
                idbytes[0] = id0;
                idbytes[1] = id1;
                idbytes[2] = id2;
                idbytes[3] = id3;

                png.WriteInt32BE(data.Length);
                png.Write(idbytes);
                png.Write(data);
                png.WriteInt32BE((int)Crc(idbytes, data));
            }

            public void Dispose() => stream.Dispose();
        }

        static Span<byte> ApplyFilters(Span<byte> prev, Span<byte> curr, int width, int bpp, byte[] buffer, out FilterType filter)
        {
            int stride = width * bpp;

            Span<byte> sub = buffer.AsSpan().Slice(0, stride);
            Span<byte> up = buffer.AsSpan().Slice(stride, stride);
            Span<byte> average = buffer.AsSpan().Slice(stride * 2, stride);
            Span<byte> paeth = buffer.AsSpan().Slice(stride * 3, stride);

            long subDiff = 0;
            long upDiff = 0;
            long avgDiff = 0;
            long paethDiff = 0;

            for (int i = 0; i < curr.Length; i++)
            {
                var p = (i - bpp >= 0) ? curr[i - bpp] : (byte)0;
                var pl = (i - bpp >= 0) ? prev[i - bpp] : (byte)0;


                sub[i] = (byte)((curr[i] - p) & 0xFF);
                up[i] = (byte)((curr[i] - prev[i]) & 0xFF);
                average[i] = (byte)((curr[i] - ((p + prev[i]) >> 1)) & 0xFF);
                paeth[i] = (byte)((curr[i] - PaethPredictor(p, prev[i], pl)) & 0xFF);

                subDiff += Math.Abs((int)(sbyte)sub[i]);
                upDiff += Math.Abs((int)(sbyte)up[i]);
                avgDiff += Math.Abs((int)(sbyte)average[i]);
                paethDiff += Math.Abs((int)(sbyte)paeth[i]);
            }

            long c = subDiff;
            filter = FilterType.Sub;
            Span<byte> ret = sub;
            if (upDiff < c) {
                c = upDiff;
                filter = FilterType.Up;
                ret = up;
            }
            if (avgDiff < c) {
                c = avgDiff;
                filter = FilterType.Average;
                ret = average;
            }
            if (paethDiff < c)
            {
                filter = FilterType.Paeth;
                ret = paeth;
            }
            return ret;
        }


		static byte PaethPredictor(byte a, byte b, byte c)
		{
			int pa = Math.Abs(b - c);
			int pb = Math.Abs(a - c);
			int pc = Math.Abs(a + b - c - c);
			if (pc < pa && pc < pb) return c;
			else if (pb < pa) return b;
			else return a;
		}
	}
}
