// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using SharpFont;
using LibreLancer.Platforms.Win32;
namespace LibreLancer.Platforms
{
	class Win32Platform : IPlatform
	{
		public bool IsDirCaseSensitive(string directory)
		{
			return false;
		}

		Face arialUnicode;
		public Face GetFallbackFace(Library library, uint cp)
		{
			if (arialUnicode == null)
			{
				var style = FontStyles.Regular;
				arialUnicode = LoadSystemFace(library, "Arial Unicode MS", ref style);
			}
			return arialUnicode;
		}

		public Face LoadSystemFace (Library library, string face, ref FontStyles style)
		{
			int weight = GDI.FW_REGULAR;
			uint fdwItalic = 0;
			//Map style
			if ((style & FontStyles.Bold) == FontStyles.Bold)
				weight = GDI.FW_BOLD;
			if ((style & FontStyles.Italic) == FontStyles.Italic)
				fdwItalic = 1;
			//Get font data from GDI
			byte[] buffer;
			unsafe {
				var hfont = GDI.CreateFont (0, 0, 0, 0, weight,
					fdwItalic, 0, 0, GDI.DEFAULT_CHARSET, GDI.OUT_OUTLINE_PRECIS,
					GDI.CLIP_DEFAULT_PRECIS, GDI.DEFAULT_QUALITY,
					GDI.DEFAULT_PITCH, face);
				//get data
				var hdc = GDI.CreateCompatibleDC(IntPtr.Zero);
				GDI.SelectObject (hdc, hfont);
				var size = GDI.GetFontData (hdc, 0, 0, IntPtr.Zero, 0);
				buffer = new byte[size];
				fixed(byte* ptr = buffer) {
					GDI.GetFontData (hdc, 0, 0, (IntPtr)ptr, size);
				}
				GDI.DeleteDC (hdc);
				//delete font
				GDI.DeleteObject (hfont);
			}
			//create font object
			//TODO: Handle multi-face data on Windows. (Is that a thing?)
			var fc = new Face(library, buffer,0);
			//Extract style
			var fs = FontStyles.Regular;
			if ((fc.StyleFlags & StyleFlags.Bold) != 0) fs |= FontStyles.Bold;
			if ((fc.StyleFlags & StyleFlags.Italic) != 0) fs |= FontStyles.Italic;
			style = fs;
			//return font
			return fc;
		}
	}
}

