using System;
using System.Collections.Generic;
using SharpFont;
using OpenTK.Graphics.OpenGL;
namespace LibreLancer
{
	public class Font
	{
		const int TEXTURE_SIZE = 1024;

		List<Texture2D> textures = new List<Texture2D>();
		Dictionary<uint, GlyphInfo> glyphs = new Dictionary<uint, GlyphInfo>();
		int currentX = 0;
		int currentY = 0;
		int lineMax = 0;
		float lineHeight;
		internal Face Face;
		string facename;
		float facesize;
		public float LineHeight {
			get {
				return lineHeight;
			}
		}
		public Font (TextRenderer t, string filename, float size)
			: this (t, new Face(t.FT, filename), size)
		{
		}

		public static Font FromSystemFont(TextRenderer t, string name, float size)
		{
			var face = Platform.LoadSystemFace (t.FT, name);
			return new Font (t, face, size);
		}

		private Font(TextRenderer t, Face f, float sz)
		{
			this.Face = f;
			f.SetCharSize (0, sz, 0, 96);
			lineHeight = (float)Face.Size.Metrics.Height;
			textures.Add (new Texture2D (
				TEXTURE_SIZE,
				TEXTURE_SIZE,
				false,
				SurfaceFormat.R8
			));
			facename = Face.FamilyName;
			facesize = sz;
			//Generate standard ASCII
			for (int i = 32; i < 127; i++) {
				AddCharacter ((uint)i);
			}
		}

		internal GlyphInfo GetGlyph(uint codepoint)
		{
			if (!glyphs.ContainsKey (codepoint))
				AddCharacter (codepoint);
			return glyphs [codepoint];
		}

		unsafe void AddCharacter(uint cp)
		{
			if (cp == (uint)'\t') {
				var spaceGlyph = GetGlyph ((uint)' ');
				glyphs.Add (cp, new GlyphInfo (spaceGlyph.AdvanceX * 4, spaceGlyph.AdvanceY, spaceGlyph.CharIndex, spaceGlyph.Kerning));
			}
			uint index = Face.GetCharIndex (cp);

			if (index == 0) {
				//Glyph does not exist in font
				if (cp == (uint)'?')
					throw new Exception ("Font does not have required ASCII character '?'");
				var qmGlyph = GetGlyph ((uint)'?');
				glyphs.Add (cp, qmGlyph);
				return;
			}
			Face.LoadGlyph (index, LoadFlags.Default | LoadFlags.ForceAutohint, LoadTarget.Normal);
			Face.Glyph.RenderGlyph (RenderMode.Normal);
			if (Face.Glyph.Bitmap.Width == 0 || Face.Glyph.Bitmap.Rows == 0) {
				glyphs.Add (cp,
					new GlyphInfo (
						(int)Math.Ceiling((float)Face.Glyph.Advance.X),
						(int)Math.Ceiling((float)Face.Glyph.Advance.Y),
						index,
						Face.HasKerning
					)
				);
			} else {
				if (Face.Glyph.Bitmap.PixelMode != PixelMode.Gray)
					throw new NotImplementedException ();
				//Continue
				if (currentX + Face.Glyph.Bitmap.Width > TEXTURE_SIZE) {
					currentX = 0;
					currentY += lineMax;
					lineMax = 0;
				}
				if (currentY + Face.Glyph.Bitmap.Rows > TEXTURE_SIZE) {
					currentX = 0;
					currentY = 0;
					lineMax = 0;
					textures.Add (new Texture2D (
						TEXTURE_SIZE,
						TEXTURE_SIZE,
						false,
						SurfaceFormat.R8
					));
					FLLog.Debug ("Text", string.Format ("{0}@{1}, New Texture", facename, facesize));
				}
				lineMax = (int)Math.Max (lineMax, Face.Glyph.Bitmap.Rows);
				var rect = new Rectangle (
					           currentX,
					           currentY,
					           Face.Glyph.Bitmap.Width,
					           Face.Glyph.Bitmap.Rows
				           );
				var tex = textures [textures.Count - 1];
				GL.PixelStore (PixelStoreParameter.UnpackAlignment, 1);
				tex.SetData (0, rect, Face.Glyph.Bitmap.Buffer);
				GL.PixelStore (PixelStoreParameter.UnpackAlignment, 4);
				currentX += Face.Glyph.Bitmap.Width;
				//tex.SetData (0, rect, Face.Glyph.Bitmap.Buffer,0, Face.Glyph.Bitmap.Width * Face.Glyph.Bitmap.Rows);
				glyphs.Add (
					cp,
					new GlyphInfo (
						tex,
						rect,
						(int)Math.Ceiling((float)Face.Glyph.Advance.X),
						(int)Math.Ceiling((float)Face.Glyph.Advance.Y),
						(int)Math.Ceiling((float)Face.Glyph.Metrics.HorizontalAdvance),
						Face.Glyph.BitmapLeft,
						Face.Glyph.BitmapTop,
						index,
						Face.HasKerning
					)
				);
			}
		}
	}
}

