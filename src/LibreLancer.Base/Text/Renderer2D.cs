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
using SharpFont;
using LibreLancer.Vertices;

namespace LibreLancer
{
	public class Renderer2D : IDisposable
	{
		const int MAX_GLYPHS = 256; //256 rendered glyphs per drawcall
		const int MAX_VERT = MAX_GLYPHS * 4;
		const int MAX_INDEX = MAX_GLYPHS * 6;

		const string vertex_source = @"
		#version 140
		in vec2 vertex_position;
		in vec2 vertex_texture1;
		in vec4 vertex_color;
		out vec2 out_texcoord;
		out vec4 blendColor;
		uniform mat4 modelviewproj;
		void main()
		{
    		gl_Position = modelviewproj * vec4(vertex_position, 0.0, 1.0);
    		blendColor = vertex_color;
    		out_texcoord = vertex_texture1;
		}
		";

		const string text_fragment_source = @"
		#version 140
		in vec2 out_texcoord;
		in vec4 blendColor;
		out vec4 out_color;
		uniform sampler2D tex;
		void main()
		{
			float alpha = texture(tex, out_texcoord).r;
			out_color = vec4(blendColor.xyz, blendColor.a * alpha);
		}
		";
		
		const string img_fragment_source = @"
		#version 140
		in vec2 out_texcoord;
		in vec4 blendColor;
		out vec4 out_color;
		uniform sampler2D tex;
		void main()
		{
			vec4 src = texture(tex, out_texcoord);
			out_color = src * blendColor;
		}
		";
		
		[StructLayout(LayoutKind.Sequential)]
		struct Vertex2D : IVertexType {
			public Vector2 Position;
			public Vector2 TexCoord;
			public Color4 Color;

			public Vertex2D(Vector2 position, Vector2 texcoord, Color4 color)
			{
				Position = position;
				TexCoord = texcoord;
				Color = color;
			}

			public VertexDeclaration GetVertexDeclaration()
			{
				return new VertexDeclaration (
					sizeof(float) * 2 + sizeof(float) * 2 + sizeof(float) * 4,
					new VertexElement (VertexSlots.Position, 2, VertexElementType.Float, false, 0),
					new VertexElement (VertexSlots.Texture1, 2, VertexElementType.Float, false, sizeof(float) * 2),
					new VertexElement (VertexSlots.Color, 4, VertexElementType.Float, false, sizeof(float) * 4)
				);
			}
		}
		
		internal Library FT;
		RenderState rs;
		VertexBuffer vbo;
		ElementBuffer el;
		Vertex2D[] vertices;
		Shader textShader;
		Shader imgShader;
		Texture2D dot;
		public Renderer2D (RenderState rstate)
		{
			rs = rstate;
			FT = new Library ();
			textShader = new Shader (vertex_source, text_fragment_source);
			textShader.SetInteger (textShader.GetLocation("tex"), 0);
			imgShader = new Shader (vertex_source, img_fragment_source);
			imgShader.SetInteger (imgShader.GetLocation("tex"), 0);
			vbo = new VertexBuffer (typeof(Vertex2D), MAX_VERT, true);
			el = new ElementBuffer (MAX_INDEX);
			var indices = new ushort[MAX_INDEX];
			vertices = new Vertex2D[MAX_VERT];
			int iptr = 0;
			for (int i = 0; i < MAX_VERT; i += 4) {
				/* Triangle 1 */
				indices[iptr++] = (ushort)i;
				indices[iptr++] = (ushort)(i + 1);
				indices[iptr++] = (ushort)(i + 2);
				/* Triangle 2 */
				indices[iptr++] = (ushort)(i + 1);
				indices[iptr++] = (ushort)(i + 3);
				indices[iptr++] = (ushort)(i + 2);
			}
			el.SetData (indices);
			vbo.SetElementBuffer (el);
			dot = new Texture2D (1, 1, false, SurfaceFormat.R8);
			dot.SetData (new byte[] { 255 });
		}

		public Point MeasureString(Font font, string str)
		{
			if (str == "") //skip empty str
				return new Point (0, 0);
			var iter = new CodepointIterator (str);
			float maxX = 0;
			float maxY = 0;
			float penX = 0, penY = 0;
			while (iter.Iterate ()) {
				uint c = iter.Codepoint;
                if (c == (uint)'\r') //Skip CR in windows CRLF combo
                {
                    continue;
                }
				if (c == (uint)'\n') {
					penY += font.LineHeight;
					penX = 0;
					maxY = 0;
					continue;
				}
				var glyph = font.GetGlyph (c);
				if (glyph.Render) {
					penX += glyph.HorizontalAdvance;
					penY += glyph.AdvanceY;
				} else {
					penX += glyph.AdvanceX;
					penY += glyph.AdvanceY;
				}
				if (glyph.Kerning && iter.Index < iter.Count - 1) {
					var g2 = font.GetGlyph (iter.PeekNext ());
					var kerning = font.Face.GetKerning (glyph.CharIndex, g2.CharIndex, KerningMode.Default);
					penX += (float)kerning.X;
				}
				maxX = Math.Max (maxX, penX);
				maxY = Math.Max (maxY, glyph.Rectangle.Height);
			}
			return new Point ((int)maxX, (int)(penY + maxY));
		}

		bool active = false;
		int vertexCount = 0;
		int primitiveCount = 0;
		Texture2D currentTexture = null;
		Shader currentShader = null;
		BlendMode currentMode = BlendMode.Normal;
		int vpHeight;
		public void Start(int vpWidth, int vpHeight)
		{
			if (active)
				throw new InvalidOperationException ("Renderer2D.Start() called without calling Renderer2D.Finish()");
			active = true;
			this.vpHeight = vpHeight;
			var mat = Matrix4.CreateOrthographicOffCenter (0, vpWidth, vpHeight, 0, 0, 1);
			textShader.SetMatrix (textShader.GetLocation("modelviewproj"), ref mat);
			imgShader.SetMatrix (imgShader.GetLocation("modelviewproj"), ref mat);
			currentMode = BlendMode.Normal;
		}

		public void DrawWithClip(Rectangle clip, Action drawfunc)
		{
			if (!active)
				throw new InvalidOperationException("Renderer2D.Start() must be called before Renderer2D.DrawWithClip()");
			Flush();
			GL.Enable(GL.GL_SCISSOR_TEST);
			GL.Scissor(clip.X, vpHeight - clip.Y - clip.Height, clip.Width, clip.Height);
			drawfunc();
			Flush();
			GL.Disable(GL.GL_SCISSOR_TEST);
		}

		public void DrawString(Font font, string str, Vector2 vec, Color4 color)
		{
			DrawString (font, str, vec.X, vec.Y, color);
		}

		public void DrawStringBaseline(Font font, string text, float x, float y, float start_x, Color4 color, bool underline = false)
		{
			if (!active)
				throw new InvalidOperationException("Renderer2D.Start() must be called before Renderer2D.DrawString");
			if (text == "") //skip empty str
				return;
			float dy = y;
			int start = 0;
			float dX = x;
			for (int i = 0; i < text.Length; i++)
			{
				if (text[i] == '\n')
				{
					DrawStringInternal(font, text, start, i, dX, dy, color, underline, true);
					dX = start_x;
					dy += font.LineHeight;
					i++;
					start = i;
				}
			}
			if (start < text.Length)
			{
				DrawStringInternal(font, text, start, text.Length, dX, dy, color, underline, true);
			}
		}

		public void DrawString(Font font, string text, float x, float y, Color4 color, bool underline = false)
		{
			if (!active)
				throw new InvalidOperationException ("Renderer2D.Start() must be called before Renderer2D.DrawString");
			if (text == "") //skip empty str
				return;
			float dy = y;
            int start = 0;
            for(int i = 0; i < text.Length; i++)
            {
                if(text[i] == '\n')
                {
                    DrawStringInternal(font, text, start, i, x, dy, color, underline, false);
                    dy += font.LineHeight;
                    i++;
                    start = i;
                }
            }
            if(start < text.Length)
            {
                DrawStringInternal(font, text, start, text.Length, x, dy, color, underline, false);
            }
		}

		static int GetAscender(Font font)
		{
			return font.Face.Size.Metrics.Ascender.ToInt32();
		}

		void DrawStringInternal(Font font, string str, int start, int end, float x, float y, Color4 color, bool underline, bool baseline)
		{
			int maxHeight = 0;
			if (!baseline)
			{
				var measureIter = new CodepointIterator(str, start, end);
				while (measureIter.Iterate())
				{
					uint c = measureIter.Codepoint;
					var glyph = font.GetGlyph(c);
					maxHeight = Math.Max(maxHeight, glyph.Rectangle.Height);
				}
			}
			var asc = GetAscender(font);
			var iter = new CodepointIterator (str, start, end);
			float penX = x, penY = y;
			while (iter.Iterate ()) {
				uint c = iter.Codepoint;
                if(c == (uint)'\r') //Skip CR from CRLF
                {
                    continue;
                }
				var glyph = font.GetGlyph (c);
				if (glyph.Render) {
					int py = baseline ? (int)penY + asc - glyph.YOffset : (int)penY + maxHeight - glyph.YOffset;
					var dst = new Rectangle (
						(int)penX + glyph.XOffset,
						py,
						glyph.Rectangle.Width,
						glyph.Rectangle.Height
					);
					DrawQuad (
						textShader,
						glyph.Texture,
						glyph.Rectangle,
						dst,
						color,
						BlendMode.Normal
					);
					penX += glyph.HorizontalAdvance;
					//penY += glyph.AdvanceY;
				} else {
					penX += glyph.AdvanceX;
					//penY += glyph.AdvanceY;
				}
				if (glyph.Kerning && iter.Index < iter.Count - 1) {
					var g2 = font.GetGlyph (iter.PeekNext ());
					var kerning = font.Face.GetKerning (glyph.CharIndex, g2.CharIndex, KerningMode.Default);
					penX += (float)kerning.X;
				}
			}
			if (underline)
			{
				//TODO: This is probably not the proper way to draw underline, but it seems to work for now
				float width = penX - x;
				var ypos = asc + 2;
				FillRectangle(new Rectangle((int)x, (int)y + ypos, (int)width, 1), color);
			}
		}
		public void FillRectangle(Rectangle rect, Color4 color)
		{
			DrawQuad(textShader, dot, new Rectangle(0,0,1,1), rect, color, BlendMode.Normal);
		}

		public void DrawRectangle(Rectangle rect, Color4 color, int width)
		{
			FillRectangle(new Rectangle(rect.X, rect.Y, rect.Width, width), color);
			FillRectangle(new Rectangle(rect.X, rect.Y, width, rect.Height), color);
			FillRectangle(new Rectangle(rect.X, rect.Y + rect.Height - width, rect.Width, width), color);
			FillRectangle(new Rectangle(rect.X + rect.Width - 2, rect.Y, width, rect.Height), color);
		}

		public void FillRectangleMask(Texture2D mask, Rectangle src, Rectangle dst, Color4 color)
		{
			DrawQuad(textShader, mask, src, dst, color, BlendMode.Normal);
		}

		public void DrawImageStretched(Texture2D tex, Rectangle dest, Color4 color, bool flip = false)
		{
			DrawQuad (
				imgShader,
				tex,
				new Rectangle (0, 0, tex.Width, tex.Height),
				dest,
				color,
				BlendMode.Normal,
				flip
			);
		}
		void Swap<T>(ref T a, ref T b)
		{
			var temp = a;
			a = b;
			b = temp;
		}

		public void Draw(Texture2D tex, Rectangle source, Rectangle dest, Color4 color, BlendMode mode = BlendMode.Normal, bool flip = false)
		{
			DrawQuad(imgShader, tex, source, dest, color, mode, flip);
		}

		void DrawQuad(Shader shader, Texture2D tex, Rectangle source, Rectangle dest, Color4 color, BlendMode mode, bool flip = false)
		{
			if (currentShader != null && currentShader != shader)
			{
				Flush();
			}
			if (currentMode != mode) {
				Flush();
			}
			if (currentTexture != null && currentTexture != tex) {
				Flush ();
			}
			if ((primitiveCount + 2) * 3 >= MAX_INDEX || (vertexCount + 4) >= MAX_VERT)
				Flush ();
			
			currentTexture = tex;
			currentShader = shader;
			currentMode = mode;

			float x = (float)dest.X;
			float y = (float)dest.Y;
			float w = (float)dest.Width;
			float h = (float)dest.Height;
			float srcX = (float)source.X;
			float srcY = (float)source.Y;
			float srcW = (float)source.Width;
			float srcH = (float)source.Height;

			Vector2 topLeftCoord = new Vector2 (srcX / (float)tex.Width,
				srcY / (float)tex.Height);
			Vector2 topRightCoord = new Vector2 ((srcX + srcW) / (float)tex.Width,
				srcY / (float)tex.Height);
			Vector2 bottomLeftCoord = new Vector2 (srcX / (float)tex.Width,
				(srcY + srcH) / (float)tex.Height);
			Vector2 bottomRightCoord = new Vector2 ((srcX + srcW) / (float)tex.Width,
				(srcY + srcH) / (float)tex.Height);
			if (flip) {
				Swap (ref bottomLeftCoord, ref topLeftCoord);
				Swap (ref bottomRightCoord, ref topRightCoord);
			}
			vertices [vertexCount++] = new Vertex2D (
				new Vector2 (x, y),
				topLeftCoord,
				color
			);
			vertices [vertexCount++] = new Vertex2D (
				new Vector2 (x + w, y),
				topRightCoord,
				color
			);
			vertices [vertexCount++] = new Vertex2D (
				new Vector2(x, y + h),
				bottomLeftCoord,
				color
			);
			vertices [vertexCount++] = new Vertex2D (
				new Vector2 (x + w, y + h),
				bottomRightCoord,
				color
			);

			primitiveCount += 2;
		}
		public void Finish()
		{
			if (!active)
				throw new InvalidOperationException ("TextRenderer.Start() must be called before TextRenderer.Finish()");
			Flush ();
			active = false;
		}

		void Flush()
		{
			if (vertexCount == 0 || primitiveCount == 0)
				return;
			rs.Cull = false;
			rs.BlendMode = currentMode;
			rs.DepthEnabled = false;
			currentTexture.BindTo (0);
			currentShader.UseProgram ();
			vbo.SetData (vertices, vertexCount);
			vbo.Draw (PrimitiveTypes.TriangleList, primitiveCount);

			vertexCount = 0;
			primitiveCount = 0;
			currentTexture = null;
			rs.Cull = true;
		}

		public void Dispose()
		{
			el.Dispose ();
			vbo.Dispose ();
			FT.Dispose ();
		}
	}
}

