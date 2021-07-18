// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;

namespace LibreLancer
{
	public static class DebugDrawing
    {
        private static Texture2D debugFont;
		public static void DrawShadowedText(Renderer2D trender, string text, float x, float y, Color4? col = null)
		{
            if (debugFont == null)
            {
                using (var stream =
                    typeof(DebugDrawing).Assembly.GetManifestResourceStream(
                        "LibreLancer.Interface.LiberationSans_0.png"))
                {
                    debugFont = (Texture2D)ImageLib.Generic.FromStream(stream, false);
                }
            }
            Color4 color = col ?? Color4.White;
            int dX = (int) x;
            int dY = (int) y;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == ' ')
                {
                    dX += DebugFont.Glyphs[' '].XAdvance;
                } else if (text[i] == '\t')
                {
                    dX += DebugFont.Glyphs[' '].XAdvance * 4;
                } 
                else if (text[i] == '\n')
                {
                    dX = (int) x;
                    dY += DebugFont.LineHeight;
                }
                else
                {
                    DebugFont.DebugGlyph glyph;
                    if (!DebugFont.Glyphs.TryGetValue(text[i], out glyph))
                        glyph = DebugFont.Glyphs['?'];
                    trender.Draw(debugFont, glyph.Source,
                        new Rectangle(dX+2+glyph.XOffset, dY+2+glyph.YOffset, glyph.Source.Width, glyph.Source.Height), Color4.Black, BlendMode.Normal, false);
                    trender.Draw(debugFont, glyph.Source,
                        new Rectangle(dX+glyph.XOffset, dY+glyph.YOffset, glyph.Source.Width, glyph.Source.Height), color, BlendMode.Normal, false);
                    dX += glyph.XAdvance;
                }
            }
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
