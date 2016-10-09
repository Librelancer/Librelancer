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
					var compress = new zlib.ZOutputStream(chnk.BaseStream, zlib.zlibConst.Z_DEFAULT_COMPRESSION);
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
						FilterType t;
						Filter(data, buf, (y + 1) * width * 4, y * width * 4, width * 4, out t);
						compress.WriteByte((byte)t);
						compress.Write(buf, 0, buf.Length);
					}
					compress.finish();
				});
				WriteChunk("IEND", writer, (chnk) => { });
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

		static ApplyFilter[] filters = new ApplyFilter[] {
			ApplyNone, ApplySub, ApplyUp, ApplyAverage, ApplyPaeth
		};

		static void Filter(byte[] data, byte[] bufa, int prev, int curr, int count, out FilterType type)
		{
			int score = int.MaxValue;
			type = FilterType.None;
			byte[] bufb = new byte[count];
			foreach (var f in filters) {
				int sc;
				FilterType ftype;
				f(data, prev, curr, count, bufb, out sc, out ftype);
				if (sc < score) {
					score = sc;
					type = ftype;
					bufb.CopyTo(bufa, 0);
				}
			}
		}
		delegate void ApplyFilter(byte[] data, int prev, int curr, int count, byte[] buf, out int score, out FilterType ft);
		static void ApplyNone(byte[] data, int prev, int curr, int count, byte[] buf, out int score, out FilterType ft)
		{
			ft = FilterType.None;
			score = 0;
			int j = 0;
			int last = 0;
			for (int i = curr; i < curr + count; i++)
			{
				var r = data[i];
				score += Math.Abs(last - r);
				last = r;
				buf[j++] = data[i];
			}
		}
		static void ApplySub(byte[] data, int prev, int curr, int count, byte[] buf, out int score, out FilterType ft)
		{
			ft = FilterType.Sub;
			score = 0;
			int j = 0;
			int last = 0;
			for (int i = 0; i < count; i++) {
				var p = (i - 4 >= 0) ? data[curr + (i - 4)] : 0;
				var r = (byte)((data[curr + i] - p) % 256);
				score += Math.Abs(last - r);
				last = r;
				buf[j++] = r;
			}
		}
		static void ApplyUp(byte[] data, int prev, int curr, int count, byte[] buf, out int score, out FilterType ft)
		{
			ft = FilterType.Up;
			score = 0;
			int j = 0;
			int last = 0;
			for (int i = 0; i < count; i++)
			{
				var r = (byte)((data[curr + i] - data[prev + i]) % 256);
				score += Math.Abs(last - r);
				last = r;
				buf[j++] = r;
			}
		}
		static void ApplyAverage(byte[] data, int prev, int curr, int count, byte[] buf, out int score, out FilterType ft)
		{
			ft = FilterType.Average;
			score = 0;
			int j = 0;
			int last = 0;
			for (int i = 0; i < count; i++) {
				var p = (i - 4 >= 0) ? data[curr + (i - 4)] : 0;
				var r = (byte)((data[curr + i] - (int)Math.Floor((p + data[prev + i]) / 2.0)) % 256);
				score += Math.Abs(last - r);
				last = r;
				buf[j++] = r;
			}
		}
		static void ApplyPaeth(byte[] data, int prev, int curr, int count, byte[] buf, out int score, out FilterType ft)
		{
			ft = FilterType.Sub;
			score = 0;
			int j = 0;
			int last = 0;
			for (int i = 0; i < count; i++)
			{
				var p = (i - 4 >= 0) ? data[curr + (i - 4)] : 0;
				var pl = (i - 4 >= 0) ? data[prev + (i - 4)] : 0;
				var r = (byte)((data[curr + i] - PaethPredictor(p, data[prev + i], pl)) % 256);
				score += Math.Abs(last - r);
				last = r;
				buf[j++] = r;
			}
		}

	}
}
