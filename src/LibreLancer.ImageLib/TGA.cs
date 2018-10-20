// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Text;

namespace LibreLancer.ImageLib
{
	public static class TGA
	{
		public static Texture2D FromFile(string file)
		{
			using (var stream = File.OpenRead (file)) {
				return FromStream (stream);
			}
		}

		public static Texture2D FromStream(Stream stream, bool hasMipMaps = false, Texture2D target = null, int mipLevel = -1)
		{
			byte[] buffer = new byte[2];

			byte colorMapType;
			byte imageType;
			int firstEntryIndex;
			int colorMapLength;
			byte colorMapEntrySize;
			int imageWidth;
			int imageHeight;
			byte pixelDepth;

			//try
			//{
			int ID_Length = stream.ReadByte();
			colorMapType = (byte)stream.ReadByte();

			// Should only be 0 (no color-map) or 1 (color-map is included).
			if (colorMapType != 0 && colorMapType != 1) return null;

			imageType = (byte)stream.ReadByte();

			stream.Read(buffer, 0, 2);
			firstEntryIndex = BitConverter.ToUInt16(buffer, 0);

			stream.Read(buffer, 0, 2);
			colorMapLength = BitConverter.ToUInt16(buffer, 0);

			colorMapEntrySize = (byte)stream.ReadByte();

			stream.Seek(2, SeekOrigin.Current); // ignore the X Origin
			stream.Seek(2, SeekOrigin.Current); // ignore the Y Origin

			stream.Read(buffer, 0, 2);
			imageWidth = BitConverter.ToUInt16(buffer, 0);

			if (imageWidth == 0) throw new Exception(); //return null;

			stream.Read(buffer, 0, 2);
			imageHeight = BitConverter.ToUInt16(buffer, 0);

			if (imageHeight == 0) throw new Exception(); //return null;

			pixelDepth = (byte)stream.ReadByte();

			stream.Seek(1, SeekOrigin.Current); // ignore the Image Descriptor

			// Skip the Image ID.
			while (ID_Length-- != 0) stream.Seek(1, SeekOrigin.Current);

			// Verify there's enough data.
			int len = (int)(stream.Length - stream.Position);
			len -= colorMapLength * ((colorMapEntrySize + 7) / 8);

			if (len <= 0) throw new Exception(); //return null;

			if (imageWidth * imageHeight * ((pixelDepth + 7) / 8) > len) throw new Exception(); //return null;
			//}
			//catch
			//{
			//throw new Exception(); //return null;
			//}

			// Only handle uncompressed mapped and RGB images.
			if (imageType == 1)
			{
				if (firstEntryIndex != 0 ||
					colorMapEntrySize != 24 ||
					pixelDepth != 8)
					throw new Exception(); //return null;
			}
			else if (imageType == 2)
			{
				if (pixelDepth != 16 && pixelDepth != 24 && pixelDepth != 32)
					throw new Exception(); //return null;
			}
			else
			{
				throw new Exception(); //return null;
			}
			int stride = (4 * imageWidth);
			int bytes = stride * imageHeight;
			byte[] pdata = new byte[bytes];

			// Process the image data depending on its type.
			if (imageType == 1)
			{
				int pal = (int)stream.Position;
				stream.Seek(3 * colorMapLength, SeekOrigin.Current);
				long streampos = stream.Position;
				for (int y = 0; y < imageHeight;y++ )
				{
					int p = y * stride;
					for (int x = 0; x < imageWidth; ++x)
					{
						stream.Seek(streampos, SeekOrigin.Begin);
						int c = pal + 3 * stream.ReadByte();
						streampos = stream.Position;

						stream.Seek(c, SeekOrigin.Begin);
						pdata[p++] = (byte)stream.ReadByte();
						pdata[p++] = (byte)stream.ReadByte();
						pdata[p++] = (byte)stream.ReadByte();
						pdata[p++] = 0xFF;
					}
				}
			}
			else if (pixelDepth == 16)
			{
				for (int y = 0; y < imageHeight;y++ )
				{
					int p = y * stride;
					for (int x = 0; x < imageWidth; ++x)
					{
						stream.Read(buffer, 0, 2);
						int val = BitConverter.ToUInt16(buffer, 0);
						int r = (val & 0x7C00) >> 10;
						int g = (val & 0x03E0) >> 5;
						int b = (val & 0x001F);
						pdata[p++] = (byte)((r << 3) | (r >> 2));
						pdata[p++] = (byte)((g << 3) | (g >> 2));
						pdata[p++] = (byte)((b << 3) | (b >> 2));
						pdata[p++] = 0xFF;
					}
				}
			}
			else
			{
				for (int y = 0; y < imageHeight;y++ )
				{
					int p = y * stride;
					for (int x = 0; x < imageWidth; ++x)
					{
						byte r = (byte)stream.ReadByte();
						byte g = (byte)stream.ReadByte();
						byte b = (byte)stream.ReadByte();
						byte a = 0xFF;
						if (pixelDepth == 32)
						{
							a = (byte)stream.ReadByte();
						}
						pdata [p++] = r;
						pdata [p++] = g;
						pdata [p++] = b;
						pdata [p++] = a;
					}
				}
			}
			if (target == null)
			{
				var tex = new Texture2D(imageWidth, imageHeight, hasMipMaps, SurfaceFormat.Color);
				tex.SetData(pdata);
				if (pixelDepth != 32 || imageType == 1)
					tex.WithAlpha = false;
				return tex;
			}
			else
			{
				target.SetData(mipLevel, null, pdata, 0, pdata.Length);
				return null;
			}
		}
	}
}