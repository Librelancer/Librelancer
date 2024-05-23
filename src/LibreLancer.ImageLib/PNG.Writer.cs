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
            deflate = new DeflateStream(outputStream, CompressionLevel.Optimal, true);
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
            var data = MemoryMarshal.Cast<Bgra8, byte>(buffer);

			using (var writer = new BinaryWriter(output))
			{
				writer.Write(PNG_SIGNATURE);
				using(var ihdr = new Chunk("IHDR"))
				{
					ihdr.Writer.WriteInt32BE(width);
                    ihdr.Writer.WriteInt32BE(height);
                    ihdr.Writer.Write((byte)8);
                    ihdr.Writer.Write((byte)ColorType.Rgba);
                    ihdr.Writer.Write((byte)0); //Compression method
                    ihdr.Writer.Write((byte)0); //Filter method
                    ihdr.Writer.Write((byte)0); //Interlacing
                    ihdr.WriteTo(writer);
				}
				using(var idat = new Chunk("IDAT"))
				{
					//zlib header
					//deflate compression
					Span<byte> buf = stackalloc byte[width * 4];
                    using (var compress = new ZlibCompress(idat.Writer.BaseStream))
                    {

                        //First line
                        compress.WriteByte((byte) 0);
                        for (int x = 0; x < width * 4; x++)
                        {
                            compress.WriteByte(data[x]);
                        }
                        //Filtered lines
                        for (int y = 1; y < height; y++)
                        {
                            var line = flip
                                ? (height - 1 - y)
                                : y;
                            ApplyPaeth(data, (y - 1) * width * 4, y * width * 4, width * 4, buf);
                            compress.WriteByte((byte) 4); //paeth filter
                            compress.Write(buf);
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

		static void ApplyPaeth(Span<byte> data, int prev, int curr, int count, Span<byte> buf)
		{
			int j = 0;
			int last = 0;
			for (int i = 0; i < count; i++)
			{
				var p = (i - 4 >= 0) ? data[curr + (i - 4)] : (byte)0;
				var pl = (i - 4 >= 0) ? data[prev + (i - 4)] : (byte)0;
				var r = (byte)((data[curr + i] - PaethPredictor(p, data[prev + i], pl)) % 256);
				last = r;
				buf[j++] = r;
			}
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
