// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using SharpFont;

namespace LibreLancer
{
	class GlyphCollection
	{
		public Font Font;
		public int Size;
		public int Ascender;
		public float LineHeight;
		public Dictionary<uint, GlyphInfo> glyphs = new Dictionary<uint, GlyphInfo>();
		internal GlyphInfo GetGlyph(uint codepoint)
		{
			if (!glyphs.ContainsKey(codepoint))
				Font.AddCharacter(this, codepoint);
			return glyphs[codepoint];
		}
	}
	public class Font : IDisposable
	{
		const int TEXTURE_SIZE = 1024;

		List<Texture2D> textures = new List<Texture2D>();
		Dictionary<int, GlyphCollection> glyphs = new Dictionary<int, GlyphCollection>();
		int currentX = 0;
		int currentY = 0;
		int lineMax = 0;
		float lineHeight;
		internal Face Face;
		string facename;
		FontStyles style;
		bool emulate_bold = false;
		bool emulate_italics = false;
		Renderer2D ren;


		public float LineHeight(float sz) 
		{
			var r = (int)Math.Round(sz);
			if (r % 2 != 0) r--; //Every 2 pixel sizes
			GlyphCollection g;
			if (!glyphs.TryGetValue(r, out g))
			{
				Face.SetCharSize(0, r, 0, 96);
				g = new GlyphCollection();
				g.Font = this;
				g.Size = r;
				g.LineHeight = (float)Face.Size.Metrics.Height;
				g.Ascender = Face.Size.Metrics.Ascender.ToInt32();
				glyphs.Add(r, g);
			}
			return g.LineHeight;
		}

		public Font (Renderer2D t, string filename, bool bold = false, bool italic = false)
			: this (t, new Face(t.FT, filename), bold, italic)
		{
		}

		public static Font FromSystemFont(Renderer2D t, string name, FontStyles styles = FontStyles.Regular)
		{
			FontStyles s = styles;
			var face = Platform.LoadSystemFace(t.FT, name, ref s);
			bool emulate_bold = false;
			bool emulate_italics = false;
			if (s != styles)
			{
				switch (styles)
				{
					case FontStyles.Bold:
						emulate_bold = true;
						break;
					case FontStyles.Italic:
						emulate_italics = true;
						break;
					case FontStyles.Bold | FontStyles.Italic:
						emulate_bold = s != FontStyles.Bold;
						emulate_italics = s != FontStyles.Italic;
						break;
				}
			}
			return new Font(t, face, emulate_bold, emulate_italics);
		}

		private Font(Renderer2D t, Face f, bool bold, bool italic)
		{
			ren = t;
			emulate_bold = bold;
			emulate_italics = italic;
			this.Face = f;
			textures.Add (new Texture2D (
				TEXTURE_SIZE,
				TEXTURE_SIZE,
				false,
				SurfaceFormat.R8
			));
			facename = Face.FamilyName;
		}

		internal GlyphCollection GetGlyphs(float size)
		{
			int r = (int)Math.Round(size);
			if (r % 2 != 0) r--; //Every 2 pixel sizes
			GlyphCollection g;
			if (!glyphs.TryGetValue(r, out g))
			{
				Face.SetCharSize(0, r, 0, 96);
				g = new GlyphCollection();
				g.Font = this;
				g.Size = r;
				g.LineHeight = (float)Face.Size.Metrics.Height;
				g.Ascender = Face.Size.Metrics.Ascender.ToInt32();
				glyphs.Add(r, g);
			}
			return g;
		}

		internal unsafe void AddCharacter(GlyphCollection col, uint cp)
		{
			if (cp == (uint)'\t') {
				var spaceGlyph = col.GetGlyph ((uint)' ');
				col.glyphs.Add (cp, new GlyphInfo (spaceGlyph.AdvanceX * 4, spaceGlyph.AdvanceY, spaceGlyph.CharIndex, spaceGlyph.Kerning));
			}
			Face.SetCharSize(0, col.Size, 0, 96);
			var c_face = Face;
			bool dobold = emulate_bold;
			bool doitalics = emulate_italics;
			bool dokern = true;
			uint index = c_face.GetCharIndex (cp);

			if (index == 0) {
				//Glyph does not exist in font
				if (cp == (uint)'?')
					throw new Exception ("Font does not have required ASCII character '?'");
				var fallback = Platform.GetFallbackFace(ren.FT, cp);
				if ((index = fallback.GetCharIndex(cp)) != 0)
				{
					try
					{
						c_face = fallback;
						c_face.SetCharSize(0, col.Size, 0, 96);
						dobold = doitalics = dokern = false;
					}
					catch (Exception)
					{
						var qmGlyph = col.GetGlyph((uint)'?');
						col.glyphs.Add(cp, qmGlyph);
						return;
					}
				}
				else
				{
					var qmGlyph = col.GetGlyph((uint)'?');
					col.glyphs.Add(cp, qmGlyph);
					return;
				}
			}
			c_face.LoadGlyph (index, LoadFlags.Default | LoadFlags.ForceAutohint, LoadTarget.Normal);
			if (dobold) {
				//Automatically determine a strength
				var strength = (c_face.UnitsPerEM * c_face.Size.Metrics.ScaleY.Value) / 0x10000;
				strength /= 24;
				c_face.Glyph.Outline.Embolden(Fixed26Dot6.FromRawValue(strength));
			}
			if (doitalics) {
				c_face.Glyph.Outline.Transform(new FTMatrix(0x10000, 0x0366A, 0x00000, 0x10000));
			}
			c_face.Glyph.RenderGlyph (RenderMode.Normal);
			if (c_face.Glyph.Bitmap.Width == 0 || c_face.Glyph.Bitmap.Rows == 0) {
				col.glyphs.Add (cp,
					new GlyphInfo (
						(int)Math.Ceiling((float)c_face.Glyph.Advance.X),
						(int)Math.Ceiling((float)c_face.Glyph.Advance.Y),
						index,
						dokern && Face.HasKerning
					)
				);
			} else {
				if (c_face.Glyph.Bitmap.PixelMode != PixelMode.Gray)
					throw new NotImplementedException ();
				if (currentX + c_face.Glyph.Bitmap.Width > TEXTURE_SIZE) {
					currentX = 0;
					currentY += lineMax;
					lineMax = 0;
				}
				if (currentY + c_face.Glyph.Bitmap.Rows > TEXTURE_SIZE) {
					currentX = 0;
					currentY = 0;
					lineMax = 0;
					textures.Add (new Texture2D (
						TEXTURE_SIZE,
						TEXTURE_SIZE,
						false,
						SurfaceFormat.R8
					));
					FLLog.Debug ("Text", string.Format ("{0}@{1}, New Texture", facename, col.Size));
				}
				lineMax = (int)Math.Max (lineMax, c_face.Glyph.Bitmap.Rows);
				var rect = new Rectangle (
					           currentX,
					           currentY,
							   c_face.Glyph.Bitmap.Width,
							   c_face.Glyph.Bitmap.Rows
				           );
				var tex = textures [textures.Count - 1];
				GL.PixelStorei (GL.GL_UNPACK_ALIGNMENT, 1);
				tex.SetData (0, rect, c_face.Glyph.Bitmap.Buffer);
				GL.PixelStorei (GL.GL_UNPACK_ALIGNMENT, 4);
				currentX += c_face.Glyph.Bitmap.Width;
				col.glyphs.Add (
					cp,
					new GlyphInfo (
						tex,
						rect,
						(int)Math.Ceiling((float)c_face.Glyph.Advance.X),
						(int)Math.Ceiling((float)c_face.Glyph.Advance.Y),
						(int)Math.Ceiling((float)c_face.Glyph.Metrics.HorizontalAdvance),
						c_face.Glyph.BitmapLeft,
						c_face.Glyph.BitmapTop,
						index,
						dokern && Face.HasKerning
					)
				);
			}
		}
		public void Dispose()
		{
			Face.Dispose ();
			foreach (var tex in textures)
				tex.Dispose ();
		}
	}
}

