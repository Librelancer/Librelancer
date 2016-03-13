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

		public Face LoadSystemFace (Library library, string face)
		{
			byte[] buffer;
			//Get font data from GDI
			unsafe {
				var hfont = GDI.CreateFont (0, 0, 0, 0, GDI.FW_REGULAR,
					0, 0, 0, GDI.DEFAULT_CHARSET, GDI.OUT_OUTLINE_PRECIS,
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
			return new Face(library, buffer,0);
		}
	}
}

