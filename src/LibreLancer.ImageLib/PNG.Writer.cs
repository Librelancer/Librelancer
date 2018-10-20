// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibreLancer.ImageLib
{
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

		public static void Save(string filename, int width, int height, byte[] data)
		{
			using (var writer = new BinaryWriter(File.Create(filename)))
			{
				writer.Write(PNG_SIGNATURE);
				WriteChunk("IHDR", writer, (chnk) =>
				{
					chnk.WriteInt32BE(width);
					chnk.WriteInt32BE(height);
					chnk.Write((byte)8);
					chnk.Write((byte)ColorType.Rgba);
					chnk.Write((byte)0); //Compression method
					chnk.Write((byte)0); //Filter method
					chnk.Write((byte)0); //Interlacing
				});
				WriteChunk("IDAT", writer, (chnk) =>
				{
					//zlib header
					//deflate compression
					byte[] buf = new byte[width * 4];
					var compress = new zlib.ZOutputStream(chnk.BaseStream, 3);
					//First line
					compress.WriteByte((byte)0);
					for (int x = 0; x < width * 4; x++)
					{
						var b = data[width * (height - 1) * 4 + x];
						compress.WriteByte(b);
					}
					//Filtered lines
					for (int y = height - 2; y >= 0; y--)
					{
						ApplyPaeth(data, (y + 1) * width * 4, y * width * 4, width * 4, buf);
						compress.WriteByte((byte)4); //paeth filter
						compress.Write(buf, 0, buf.Length);
					}
					compress.finish();
				});
				writer.Write(IEND);
			}
		}

		static void WriteChunk(string id, BinaryWriter writer, Action<BinaryWriter> writefunc)
		{
			var idbytes = Encoding.ASCII.GetBytes(id);
			byte[] data;
			using (var strm = new MemoryStream())
			{
				writefunc(new BinaryWriter(strm));
				data = strm.ToArray();
			}
			writer.WriteInt32BE(data.Length);
			writer.Write(idbytes);
			writer.Write(data);
			writer.WriteInt32BE((int)Crc(idbytes, data));
		}

		static void ApplyPaeth(byte[] data, int prev, int curr, int count, byte[] buf)
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
