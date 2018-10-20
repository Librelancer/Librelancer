// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer
{
	public static class DebugDrawing
	{
		public static void DrawShadowedText(Renderer2D trender, Font font, float size, string text, float x, float y, Color4? col = null)
		{
			trender.DrawString(font,
			                   size,
				text,
				x + 2, y + 2,
				Color4.Black);
			trender.DrawString(font,
			                   size,
				text,
				x, y,
				col ?? Color4.White);
		}


		static readonly string[] SizeSuffixes =
				   { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
		public static string SizeSuffix(Int64 value)
		{
			if (value < 0) { return "-" + SizeSuffix(-value); }
			if (value == 0) { return "0.0 bytes"; }

			int mag = (int)Math.Log(value, 1024);
			decimal adjustedSize = (decimal)value / (1L << (mag * 10));

			return string.Format("{0:n1} {1}", adjustedSize, SizeSuffixes[mag]);
		}
	}
}
