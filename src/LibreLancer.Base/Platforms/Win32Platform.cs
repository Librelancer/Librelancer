// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Platforms.Win32;

namespace LibreLancer.Platforms
{
	class Win32Platform : IPlatform
	{
		public bool IsDirCaseSensitive(string directory)
		{
			return false;
		}

        public string GetLocalConfigFolder() => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        
        static bool GdiOpenFace(string face, out byte[] buffer)
        {
            int weight = GDI.FW_REGULAR;
            uint fdwItalic = 0;
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
        public byte[] GetMonospaceBytes()
        {
            byte[] buffer;
            if (GdiOpenFace("Consolas", out buffer)) return buffer;
            if (GdiOpenFace("Courier New", out buffer)) return buffer;
            if (GdiOpenFace("Arial", out buffer)) return buffer;
            throw new Exception("No system monospace font");
        }

        public void AddTtfFile(string file)
        {
            //Not implemented
        }
    }
}

