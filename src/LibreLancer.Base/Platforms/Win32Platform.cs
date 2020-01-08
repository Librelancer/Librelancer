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
                byte[] buffer;
                if (!GdiOpenFace("Arial Unicode MS", FontStyles.Regular, out buffer))
                {
                    if(!GdiOpenFace("Microsoft Sans Serif", FontStyles.Regular, out buffer))
                    {
                        throw new Exception("GDI Error loading fallback face");
                    }
                }
                arialUnicode = new Face(library, buffer, 0);
            }
            return arialUnicode;
		}

        static bool GdiOpenFace(string face, FontStyles style, out byte[] buffer)
        {
            int weight = GDI.FW_REGULAR;
            uint fdwItalic = 0;
            //Map style
            if ((style & FontStyles.Bold) == FontStyles.Bold)
                weight = GDI.FW_BOLD;
            if ((style & FontStyles.Italic) == FontStyles.Italic)
                fdwItalic = 1;
            //Get font data from GDI
            buffer = null;
            unsafe
            {
                var hfont = GDI.CreateFont(0, 0, 0, 0, weight,
                    fdwItalic, 0, 0, GDI.DEFAULT_CHARSET, GDI.OUT_OUTLINE_PRECIS,
                    GDI.CLIP_DEFAULT_PRECIS, GDI.DEFAULT_QUALITY,
                    GDI.DEFAULT_PITCH, face);
                //get data
                var hdc = GDI.CreateCompatibleDC(IntPtr.Zero);
                GDI.SelectObject(hdc, hfont);
                var size = GDI.GetFontData(hdc, 0, 0, IntPtr.Zero, 0);
                if (size == GDI.GDI_ERROR)
                {
                    FLLog.Warning("GDI", "GetFontData for " + face + " failed");
                    GDI.DeleteDC(hdc);
                    GDI.DeleteObject(hfont);
                    return false;
                }
                buffer = new byte[size];
                fixed (byte* ptr = buffer)
                {
                    GDI.GetFontData(hdc, 0, 0, (IntPtr)ptr, size);
                }
                GDI.DeleteDC(hdc);
                //delete font
                GDI.DeleteObject(hfont);
                return true;
            }
        }
        public Face LoadSystemFace (Library library, string face, ref FontStyles style)
		{
            //create font object
            //TODO: Handle multi-face data on Windows. (Is that a thing?)
            byte[] buffer;
            if(!GdiOpenFace(face, style, out buffer))
            {
                style = FontStyles.Regular;
                return GetFallbackFace(library, 0);
            }
            var fc = new Face(library, buffer,0);
			//Extract style
			var fs = FontStyles.Regular;
			if ((fc.StyleFlags & StyleFlags.Bold) != 0) fs |= FontStyles.Bold;
			if ((fc.StyleFlags & StyleFlags.Italic) != 0) fs |= FontStyles.Italic;
			style = fs;
			//return font
			return fc;
		}

        public byte[] GetMonospaceBytes()
        {
            byte[] buffer;
            if (GdiOpenFace("Consolas", FontStyles.Regular, out buffer)) return buffer;
            if (GdiOpenFace("Courier New", FontStyles.Regular, out buffer)) return buffer;
            if (GdiOpenFace("Arial", FontStyles.Regular, out buffer)) return buffer;
            throw new Exception("No system monospace font");
        }

        public void AddTtfFile(string file)
        {
            //Not implemented
        }
    }
}

