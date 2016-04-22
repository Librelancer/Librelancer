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
using System.Runtime.InteropServices;
namespace LibreLancer.Platforms.Win32
{
	static class GDI
	{
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
	}
}

