using System;
using System.Runtime.InteropServices;
using SharpFont;
using LibreLancer.Vertices;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

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
		uniform mat4x4 modelviewproj;
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

			public void SetVertexPointers (int offset)
			{
				GL.EnableVertexAttribArray(VertexSlots.Position);
				GL.EnableVertexAttribArray(VertexSlots.Color);
				GL.EnableVertexAttribArray(VertexSlots.Texture1);
				GL.VertexAttribPointer(VertexSlots.Position, 2, VertexAttribPointerType.Float, false, VertexSize(), offset);
				GL.VertexAttribPointer(VertexSlots.Texture1, 2, VertexAttribPointerType.Float, false, VertexSize(), offset + sizeof(float) * 2);
				GL.VertexAttribPointer(VertexSlots.Color, 4, VertexAttribPointerType.Float, false, VertexSize(), offset + sizeof(float) * 4);
			}
			public Vertex2D(Vector2 position, Vector2 texcoord, Color4 color)
			{
				Position = position;
				TexCoord = texcoord;
				Color = color;
			}
			public int VertexSize ()
			{
				return sizeof(float) * 2 +
				sizeof(float) * 2 +
				sizeof(float) * 4;
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
			textShader.SetInteger ("tex", 0);
			imgShader = new Shader (vertex_source, img_fragment_source);
			textShader.SetInteger ("tex", 0);
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

		public void Start(int vpWidth, int vpHeight)
		{
			if (active)
				throw new InvalidOperationException ("TextRenderer.Start() called without calling TextRenderer.Finish()");
			active = true;
			var mat = Matrix4.CreateOrthographicOffCenter (0, vpWidth, vpHeight, 0, 0, 1);
			textShader.SetMatrix ("modelviewproj", ref mat);
			imgShader.SetMatrix ("modelviewproj", ref mat);
		}

		public void DrawString(Font font, string str, Vector2 vec, Color4 color)
		{
			DrawString (font, str, vec.X, vec.Y, color);
		}

		public void DrawString(Font font, string text, float x, float y, Color4 color)
		{
			if (!active)
				throw new InvalidOperationException ("TextRenderer.Start() must be called before TextRenderer.DrawString");
			if (text == "") //skip empty str
				return;
			string[] split = text.Split ('\n');
			float dy = y;
			foreach(var str in split) {
				DrawStringInternal (font, str, x, dy, color);
				dy += font.LineHeight;
			}

		}
		void DrawStringInternal(Font font, string str, float x, float y, Color4 color)
		{
			var measureIter = new CodepointIterator (str);
			int maxHeight = 0;
			while (measureIter.Iterate ()) {
				uint c = measureIter.Codepoint;
				var glyph = font.GetGlyph (c);
				maxHeight = Math.Max (maxHeight, glyph.Rectangle.Height);
			}
			var iter = new CodepointIterator (str);
			float penX = x, penY = y;
			while (iter.Iterate ()) {
				uint c = iter.Codepoint;
				var glyph = font.GetGlyph (c);
				if (glyph.Render) {
					float y2 = -penY - glyph.YOffset;
					var dst = new Rectangle (
						(int)penX + glyph.XOffset,
						(int)penY + (maxHeight - glyph.YOffset),
						glyph.Rectangle.Width,
						glyph.Rectangle.Height
					);
					DrawQuad (
						textShader,
						glyph.Texture,
						glyph.Rectangle,
						dst,
						color
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
		}
		public void FillRectangle(Rectangle rect, Color4 color)
		{
			DrawQuad(textShader, dot, new Rectangle(0,0,1,1), rect, color);
		}
		public void DrawImageStretched(Texture2D tex, Rectangle dest, Color4 color, bool flip = false)
		{
			DrawQuad (
				imgShader,
				tex,
				new Rectangle (0, 0, tex.Width, tex.Height),
				dest,
				color,
				flip
			);
		}
		void Swap<T>(ref T a, ref T b)
		{
			var temp = a;
			a = b;
			b = temp;
		}
		void DrawQuad(Shader shader, Texture2D tex, Rectangle source, Rectangle dest, Color4 color, bool flip = false)
		{
			if (currentShader != null && currentShader != shader) {
				Flush ();
			}
			if (currentTexture != null && currentTexture != tex) {
				Flush ();
			}
			if ((primitiveCount + 2) * 3 >= MAX_INDEX || (vertexCount + 4) >= MAX_VERT)
				Flush ();
			
			currentTexture = tex;
			currentShader = shader;

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
			GL.Disable (EnableCap.CullFace);
			rs.BlendMode = BlendMode.Normal;
			rs.DepthEnabled = false;
			currentTexture.BindTo (TextureUnit.Texture0);
			currentShader.UseProgram ();
			vbo.SetData (vertices, vertexCount);
			vbo.Draw (PrimitiveTypes.TriangleList, primitiveCount);

			vertexCount = 0;
			primitiveCount = 0;
			currentTexture = null;
			GL.Enable (EnableCap.CullFace);
		}

		public void Dispose()
		{
			el.Dispose ();
			vbo.Dispose ();
			FT.Dispose ();
		}
	}
}

