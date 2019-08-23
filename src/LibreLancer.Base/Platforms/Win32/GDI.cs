// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.InteropServices;
namespace LibreLancer.Platforms.Win32
{
	static class GDI
	{
        public const uint GDI_ERROR = uint.MaxValue;

		[DllImport("gdi32.dll")]
		public static extern uint GetFontData (IntPtr hdc, uint dwTable, uint dwOffset, IntPtr lpvBuffer, uint cbData);
		//dwTable 0 for whole font
		[DllImport("gdi32.dll")]
		public static extern IntPtr SelectObject (IntPtr hdc, IntPtr hgdiobj);

		[DllImport("gdi32.dll")]
		public static extern IntPtr CreateFont(
			int nHeight,
			int nWidth,
			int nEscapement,
			int nOrientation,
			int fnWeight,
			uint fdwItalic,
			uint fdwUnderline,
			uint fdwStrikeOut,
			uint fdwCharSet,
			uint fdwOutputPrecision,
			uint fdwClipPrecision,
			uint fdwQuality,
			uint fdwPitchAndFamily,
			string lpszFace
		);
		[DllImport("gdi32.dll")]
		public static extern IntPtr CreateCompatibleDC (IntPtr hdc);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateSolidBrush(uint color);

        [DllImport("gdi32.dll")]
        public static extern IntPtr GetCurrentObject(IntPtr hdc, uint type);

        [DllImport("gdi32.dll")]
        public static extern int GetObject(IntPtr h, int c, out DIBSECTION pv); //LPVOID

        [DllImport("user32.dll")]
        public static extern int FillRect(IntPtr hdc, ref RECT lrpc, IntPtr hbrush);

        [DllImport("gdi32.dll")]
		public static extern bool DeleteObject(IntPtr hObject);

		[DllImport("gdi32.dll")]
		public static extern bool DeleteDC(IntPtr hdc);

		public const int FW_REGULAR = 400;
		public const int FW_BOLD = 700;
		public const uint DEFAULT_CHARSET = 1;
		public const uint OUT_OUTLINE_PRECIS = 8;
		public const uint CLIP_DEFAULT_PRECIS = 0;
		public const uint DEFAULT_QUALITY = 0;
		public const uint DEFAULT_PITCH = 0;
        public const uint OBJ_BITMAP = 7;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAP
        {
            public int bmType;
            public int bmWidth;
            public int bmHeight;
            public int bmWidthBytes;
            public short bmPlanes;
            public short bmBitsPixel;
            public IntPtr bmBits;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAPINFOHEADER
        {
            public uint biSize;
            public int biWidth;
            public int biHeight;
            public short biPlanes;
            public short biBitCount;
            public uint biCompression;
            public uint biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public uint biClrUsed;
            public uint biClrImportant;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct DIBSECTION
        {
            public BITMAP dsBm;
            public BITMAPINFOHEADER dsBmih;
            public uint dsBitfields1;
            public uint dsBitfields2;
            public uint dsBitfields3;
            public IntPtr dshSection;
            public uint dsOffset;
        }
    }
}