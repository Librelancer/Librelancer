using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
//TODO: Refactor and optimise PngLoader
namespace LibreLancer.ImageLib
{
	public static class PNG
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

		enum FilterType
		{
			None = 0,
			Sub = 1,
			Up = 2,
			Average = 3,
			Paeth = 4
		}
		public static bool StreamIsPng(Stream stream)
		{
			byte[] bytes = new byte[8];
			stream.Read (bytes, 0, 8);
			bool result = BitConverter.ToUInt64 (bytes, 0) == PNG_SIGNATURE;
			stream.Position = 0;
			return result;
		}
		public static Texture2D FromStream(Stream stream)
		{
			List<byte> idat = new List<byte> ();
			byte bitDepth = 0;
			ColorType colorType = ColorType.Grayscale;
			Color4b[] palette = null;
			int width = 0, height = 0;
			using (var reader = new BinaryReader (stream)) {
				if (reader.ReadUInt64 () != PNG_SIGNATURE) {
					throw new Exception ("Not a PNG file");
				}
				string chunktype = "";
				byte[] typeBuf = new byte[4];
				while (chunktype != "IEND") {
					var length = reader.ReadInt32BE ();
					reader.Read (typeBuf, 0, 4);
					chunktype = Encoding.ASCII.GetString (typeBuf);
					switch (chunktype) {
					case "IHDR":
						width = reader.ReadInt32BE ();
						height = reader.ReadInt32BE ();
						bitDepth = reader.ReadByte ();
						colorType = (ColorType)reader.ReadByte ();
						if (reader.ReadByte () != 0) {
							throw new Exception (); //Compression method
						}
						if (reader.ReadByte () != 0) {
							throw new Exception (); //Filter method
						}
						if (reader.ReadByte () != 0) {
							throw new NotImplementedException (); //Interlacing
						}
						break;
					case "PLTE":
						if (length % 3 != 0)
							throw new Exception (); //Invalid Palette
						int count = length / 3;
						palette = new Color4b[length / 3];
						for (int i = 0; i < count; i++) {
							palette [i] = new Color4b (
								reader.ReadByte (),
								reader.ReadByte (),
								reader.ReadByte (),
								255);
						}
						break;
					case "tRNS":
						if (colorType == ColorType.Palette) {
							for (int i = 0; i < length; i++) {
								palette [i].A = reader.ReadByte ();
							}
						} else {
							throw new NotImplementedException (); //Are the others BigEndian? Investigate
						}
						break;
					case "IDAT":
						idat.AddRange (reader.ReadBytes (length));
						break;
					default:
						reader.BaseStream.Seek (length, SeekOrigin.Current);
						break;
					}
					reader.BaseStream.Seek (4, SeekOrigin.Current); //Skip CRC
				}
			}
			byte[] decompressedBytes = null;
			using (var compressedStream = new MemoryStream (idat.ToArray (), 2, idat.Count - 6)) { //skip zlib header
				using (var decompressedStream = new MemoryStream ()) {
					try {
						using (var deflateStream = new DeflateStream (compressedStream, CompressionMode.Decompress)) {
							deflateStream.CopyTo (decompressedStream);
						}
						decompressedBytes = decompressedStream.ToArray ();
					} catch (Exception exception) {
						throw new Exception ("An error occurred during DEFLATE decompression.", exception);
					}
				}

			} 
			var scanlines = GetScanlines (decompressedBytes, bitDepth, colorType, width);
			var colors = GetData (scanlines, width, height, colorType, bitDepth, palette);
			var texture = new Texture2D (width, height);
			texture.SetData (colors);
			return texture;
		}

		static Color4b[] GetData(byte[][] scanlines, int width, int height, ColorType colorType, int bitsPerSample, Color4b[] palette)
		{
			var bytesPerPixel = BytesPerPixel(colorType, bitsPerSample);
			var bytesPerSample = bitsPerSample / 8;
			var bytesPerScanline = (bytesPerPixel * width) + 1;
			var data = new Color4b[width * height];
			byte[] previousScanline = new byte[bytesPerScanline];
			for (int y = 0; y < height; y++)
			{
				var scanline = scanlines[y];
				FilterType filterType = (FilterType)scanline[0];
				byte[] defilteredScanline;
				switch (filterType)
				{
				case FilterType.None:
					defilteredScanline = new byte[scanline.Length];
					Array.Copy (scanline, defilteredScanline, scanline.Length);
					break;

				case FilterType.Sub:
					defilteredScanline = SubDefilter(scanline, bytesPerPixel);
					break;

				case FilterType.Up:
					defilteredScanline = UpDefilter(scanline, previousScanline);
					break;

				case FilterType.Average:
					defilteredScanline = AverageDefilter(scanline, previousScanline, bytesPerPixel);
					break;

				case FilterType.Paeth:
					defilteredScanline = PaethDefilter(scanline, previousScanline, bytesPerPixel);
					break;

				default:
					throw new Exception("Unknown filter type.");
				}

				previousScanline = defilteredScanline;
				ConvertColors (defilteredScanline, y, width, colorType, bytesPerPixel, bytesPerSample, data, palette);
			}
			return data;
		}

		static void ConvertColors(byte[] defilteredScanline, int y, int width, ColorType colorType, int bytesPerPixel, int bytesPerSample, Color4b[] data, Color4b[] palette)
		{
			switch (colorType)
			{
			case ColorType.Grayscale:

				for (int x = 0; x < width; x++)
				{
					int offset = 1 + (x * bytesPerPixel);

					byte intensity = defilteredScanline[offset];

					data[(y * width) + x] = new Color4b(intensity, intensity, intensity, 255);
				}

				break;

			case ColorType.GrayscaleAlpha:

				for (int x = 0; x < width; x++)
				{
					int offset = 1 + (x * bytesPerPixel);

					byte intensity = defilteredScanline[offset];
					byte alpha = defilteredScanline[offset + bytesPerSample];

					data[(y * width) + x] = new Color4b(intensity, intensity, intensity, alpha);
				}

				break;

			case ColorType.Palette:

				for (int x = 0; x < width; x++)
				{
					var pixelColor = palette[defilteredScanline[x + 1]];

					data[(y * width) + x] = pixelColor;
				}

				break;

			case ColorType.Rgb:

				for (int x = 0; x < width; x++)
				{
					int offset = 1 + (x * bytesPerPixel);

					int blue = defilteredScanline[offset];
					int green = defilteredScanline[offset + bytesPerSample];
					int red = defilteredScanline[offset + 2 * bytesPerSample];

					data[(y * width) + x] = new Color4b((byte)red, (byte)green, (byte)blue, 255);
				}

				break;

			case ColorType.Rgba:

				for (int x = 0; x < width; x++)
				{
					int offset = 1 + (x * bytesPerPixel);

					int blue = defilteredScanline[offset];
					int green = defilteredScanline[offset + bytesPerSample];
					int red = defilteredScanline[offset + 2 * bytesPerSample];
					int alpha = defilteredScanline[offset + 3 * bytesPerSample];

					data[(y * width) + x] = new Color4b((byte)red, (byte)green, (byte)blue, (byte)alpha);
				}

				break;

			default:
				break;
			}
		}
		static int BytesPerPixel(ColorType colorType, int bitsPerSample)
		{
			switch (colorType)
			{
			case ColorType.Grayscale:
				return bitsPerSample / 8;

			case ColorType.GrayscaleAlpha:
				return (2 * bitsPerSample) / 8;

			case ColorType.Palette:
				return bitsPerSample / 8;

			case ColorType.Rgb:
				return (3 * bitsPerSample) / 8;

			case ColorType.Rgba:
				return (4 * bitsPerSample) / 8;

			default:
				throw new Exception("Unknown color type.");
			}
		}

		static byte[][] GetScanlines(byte[] pixelData, int bitDepth, ColorType colorType, int width)
		{
			var bytesPerPixel = BytesPerPixel(colorType, bitDepth);
			var bytesPerScanline = (bytesPerPixel * width) + 1;
			int scanlineCount = pixelData.Length / bytesPerScanline;

			if (pixelData.Length % bytesPerScanline != 0)
			{
				throw new Exception("Corrupt pixel data");
			}

			var result = new byte[scanlineCount][];

			for (int y = 0; y < scanlineCount; y++)
			{
				result[y] = new byte[bytesPerScanline];

				for (int x = 0; x < bytesPerScanline; x++)
				{
					result[y][x] = pixelData[y * bytesPerScanline + x];
				}
			}

			return result;
		}


		static byte[] SubDefilter(byte[] scanline, int bytesPerPixel)
		{
			byte[] result = new byte[scanline.Length];

			for (int x = 1; x < scanline.Length; x++)
			{
				byte priorRawByte = (x - bytesPerPixel < 1) ? (byte)0 : result[x - bytesPerPixel];

				result[x] = (byte)((scanline[x] + priorRawByte) % 256);
			}

			return result;
		}

		static byte[] UpDefilter(byte[] scanline, byte[] previousScanline)
		{
			byte[] result = new byte[scanline.Length];

			for (int x = 1; x < scanline.Length; x++)
			{
				byte above = previousScanline[x];

				result[x] = (byte)((scanline[x] + above) % 256);
			}

			return result;
		}

		static byte[] AverageDefilter(byte[] scanline, byte[] previousScanline, int bytesPerPixel)
		{
			byte[] result = new byte[scanline.Length];

			for (int x = 1; x < scanline.Length; x++)
			{
				byte left = (x - bytesPerPixel < 1) ? (byte)0 : result[x - bytesPerPixel];
				byte above = previousScanline[x];

				result[x] = (byte)((scanline[x] + Average(left, above)) % 256);
			}

			return result;
		}

		private static int Average(byte left, byte above)
		{
			return (int)(Math.Floor((left + above) / 2.0));
		}

		static byte[] PaethDefilter(byte[] scanline, byte[] previous, int bpp)
		{
			byte[] result = new byte[scanline.Length];

			for (int x = 1; x < scanline.Length; x++)
			{
				byte left = (x - bpp < 1) ? (byte)0 : result[x - bpp];
				byte above = previous[x];
				byte upperLeft = (x - bpp < 1) ? (byte)0 : previous[x - bpp];

				result[x] = (byte)((scanline[x] + PaethPredictor(left, above, upperLeft)) % 256);
			}

			return result;
		}

		static int PaethPredictor(int a, int b, int c)
		{
			int p = a + b - c;
			int pa = Math.Abs(p - a);
			int pb = Math.Abs(p - b);
			int pc = Math.Abs(p - c);

			if ((pa <= pb) && (pa <= pc))
			{
				return a;
			}
			else
			{
				if (pb <= pc)
				{
					return b;
				}
				else
				{
					return c;
				}
			}
		}
	}
}

