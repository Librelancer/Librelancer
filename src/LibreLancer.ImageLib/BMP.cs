/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.IO;
namespace LibreLancer.ImageLib
{
	public static class BMP
	{
		const ushort MAGIC = 0x4D42;

		public static bool StreamIsBMP(Stream stream)
		{
			var reader = new BinaryReader (stream);
			var result = reader.ReadUInt16 () == MAGIC;
			stream.Position = 0;
			return result;
		}
		public static Texture2D FromFile(string file)
		{
			using (var stream = File.OpenRead (file)) {
				return FromStream (stream);
			}
		}
		public static Texture2D FromStream(Stream stream)
		{
			var reader = new BinaryReader (stream);
			//Bitmap Header
			if (reader.ReadUInt16 () != MAGIC)
				throw new Exception ("Not a BMP file");
			reader.Skip (1 * sizeof(int) + 2 * sizeof(ushort)); //Skip file size and reserved
			uint dataOffset = reader.ReadUInt32();
			//Image Header - we're only handling one version
			if (reader.ReadUInt32 () != 40)
				throw new Exception ("Unsupported BMP version");
			var imageWidth = reader.ReadInt32 ();
			var imageHeight = reader.ReadInt32 ();
			if (reader.ReadUInt16 () != 1)
				throw new Exception ("BMP planes must == 1");
			var bpp = reader.ReadUInt16 ();
			if (reader.ReadUInt32 () != 0)
				throw new Exception ("BMP compression not supported");
			reader.Skip (sizeof(int) * 3); //image size in bytes + pixels per metre
			var colorsUsed = reader.ReadInt32();
			reader.Skip (sizeof(int)); //number of "important" colours in map
			//Color map
			Color4b[] colorMap;
			int colormapSize = 0;
			if (bpp == 1) colormapSize = 2;
			if (bpp == 4) colormapSize = colorsUsed == 0 ? 16 : colorsUsed;
			if (bpp == 8) colormapSize = colorsUsed == 0 ? 256 : colorsUsed;
			if (colormapSize != 0) {
				byte[] bytes = new byte[4];
				colorMap = new Color4b[colormapSize];
				for (int i = 0; i < colormapSize; i++) {
					reader.BaseStream.Read (bytes, 0, 4);
					colorMap [i].R = bytes [1];
					colorMap [i].G = bytes [2];
					colorMap [i].B = bytes [3];
					colorMap [i].A = 0xFF;
				}
			}
			//Read the pixel data :D
			reader.BaseStream.Seek(dataOffset, SeekOrigin.Begin);
			Color4b[] data;
			SurfaceFormat format;
			if (bpp == 24) {
				format = SurfaceFormat.Color;
				data = new Color4b[imageWidth * imageHeight];
				int rowSize = imageWidth * imageHeight * 3;
				rowSize += (rowSize % 4);
				for (int y = 0; y < imageHeight; y++) {
					int rowOffset = imageWidth * y;
					int j = 0;
					var bytes = reader.ReadBytes (rowSize);
					for (int x = 0; x < imageWidth; x++) {
						data [rowOffset + x].R = bytes [j++];
						data [rowOffset + x].G = bytes [j++];
						data [rowOffset + x].B = bytes [j++];
						data [rowOffset + x].A = 0xFF;
					}
				}
			} else {
				throw new Exception ("Only 24-bpp BMPs supported");
			}
			var tex = new Texture2D (imageWidth, imageHeight, false, format);
			tex.SetData (data);
			return tex;
		}
	}
}

