// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Runtime.InteropServices;
using LibreLancer.Vertices;

namespace LibreLancer
{
	public unsafe class Renderer2D : IDisposable
	{
		const int MAX_GLYPHS = 512; //512 rendered glyphs per drawcall
		const int MAX_VERT = MAX_GLYPHS * 4;
		const int MAX_INDEX = MAX_GLYPHS * 6;

		const string vertex_source = @"
		#version {0}
		in vec2 vertex_position;
		in vec2 vertex_texture1;
        in vec2 vertex_texture2;
		in vec4 vertex_color;
		out vec2 out_texcoord;
        out vec2 c_pos;
		out vec4 blendColor;
		uniform mat4 modelviewproj;
		void main()
		{
    		gl_Position = modelviewproj * vec4(vertex_position, 0.0, 1.0);
    		blendColor = vertex_color;
    		out_texcoord = vertex_texture1;
            c_pos = vertex_texture2;
		}
		";

        const string img_fragment_source = @"
		#version {0}
		in vec2 out_texcoord;
        in vec2 c_pos;
		in vec4 blendColor;
		out vec4 out_color;
		uniform sampler2D tex;
        uniform float blend;
        uniform bool circle;
		void main()
		{
            vec4 src = texture(tex, out_texcoord);
            src = mix(src, vec4(1.0,1.0,1.0, src.r), blend);
            if(circle) {
                vec2 val = c_pos - vec2(0.5);
                float r = sqrt(dot(val,val));
                float delta = fwidth(r);
                float alpha = smoothstep(0.5, 0.5 - delta, r);
                out_color = src * blendColor * vec4(1.0,1.0,1.0,alpha);
            } else {
                out_color = src * blendColor;
            }
		}
		";
		
		[StructLayout(LayoutKind.Sequential)]
		struct Vertex2D : IVertexType {
			public Vector2 Position;
			public Vector2 TexCoord;
            public int CircleCoord;
			public int Color;

			public Vertex2D(Vector2 position, Vector2 texcoord, Color4 color)
			{
				Position = position;
				TexCoord = texcoord;
                CircleCoord = 0;
                Color = color.ToAbgr();
            }

            public Vertex2D(Vector2 position, Vector2 texcoord, int circlecoord, Color4 color)
            {
                Position = position;
                TexCoord = texcoord;
                CircleCoord = circlecoord;
                Color = color.ToAbgr();
            }

			public VertexDeclaration GetVertexDeclaration()
			{
				return new VertexDeclaration (
					sizeof(float) * 2 + sizeof(float) * 2 + sizeof(int) + sizeof(int),
					new VertexElement (VertexSlots.Position, 2, VertexElementType.Float, false, 0),
					new VertexElement (VertexSlots.Texture1, 2, VertexElementType.Float, false, sizeof(float) * 2),
                    new VertexElement(VertexSlots.Texture2, 2, VertexElementType.UnsignedShort, false, sizeof(float) * 4),
					new VertexElement (VertexSlots.Color, 4, VertexElementType.UnsignedByte, true, sizeof(float) * 5)
				);
			}
		}
		
		RenderState rs;
		VertexBuffer vbo;
		ElementBuffer el;
        Vertex2D* vertices;
		Shader imgShader;
		Texture2D dot;
        private int blendLocation;
        private int circleLocation;
		public Renderer2D (RenderState rstate)
		{
			rs = rstate;
            string glslVer = GL.GLES ? "300 es\nprecision mediump float;" : "140";
            imgShader = new Shader (vertex_source.Replace("{0}", glslVer), img_fragment_source.Replace("{0}", glslVer));
			imgShader.SetInteger (imgShader.GetLocation("tex"), 0);
            blendLocation = imgShader.GetLocation("blend");
            circleLocation = imgShader.GetLocation("circle");
			vbo = new VertexBuffer (typeof(Vertex2D), MAX_VERT, true);
			el = new ElementBuffer (MAX_INDEX);
			var indices = new ushort[MAX_INDEX];
			//vertices = new Vertex2D[MAX_VERT];
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

        private RichTextEngine richText;
        public RichTextEngine CreateRichTextEngine()
        {
            if (richText == null)
            {
                if (Platform.RunningOS == OS.Linux)
                    richText = new Text.Pango.PangoText(this);
                else if (Platform.RunningOS == OS.Windows)
                    //Different method
                    //So we don't have to load SharpDX on linux
                    richText = DWriteEngine();
                else
                    throw new NotImplementedException();
            }
            return richText;
        }
        RichTextEngine DWriteEngine()
        {
            return new Text.DirectWrite.DirectWriteText(this);
        }

        public Point MeasureString(string fontName, float size, string str)
		{
			if (str == "" || size < 1) //skip empty str
				return new Point (0, 0);
            return CreateRichTextEngine().MeasureString(fontName, size, str);
        }

        public float LineHeight(string fontName, float size)
        {
            if (size < 1) return 0;
            return CreateRichTextEngine().LineHeight(fontName, size);
        }
        
		bool active = false;
		int vertexCount = 0;
		int primitiveCount = 0;
		Texture2D currentTexture = null;
		BlendMode currentMode = BlendMode.Normal;
        private bool isCircle = false;
		int vpHeight;
		public void Start(int vpWidth, int vpHeight)
		{
			if (active)
				throw new InvalidOperationException ("Renderer2D.Start() called without calling Renderer2D.Finish()");
			active = true;
			this.vpHeight = vpHeight;
			var mat = Matrix4x4.CreateOrthographicOffCenter (0, vpWidth, vpHeight, 0, 0, 1);
			imgShader.SetMatrix (imgShader.GetLocation("modelviewproj"), ref mat);
			currentMode = BlendMode.Normal;
            vertices = (Vertex2D*)vbo.BeginStreaming();
            isCircle = false;
            
        }

		public void DrawWithClip(Rectangle clip, Action drawfunc)
		{
			if (!active)
				throw new InvalidOperationException("Renderer2D.Start() must be called before Renderer2D.DrawWithClip()");
			Flush();
            rs.ScissorEnabled = true;
            rs.ScissorRectangle = clip;
			drawfunc();
			Flush();
            rs.ScissorEnabled = false;
		}

		public void DrawString(string fontName, float size, string str, Vector2 vec, Color4 color)
        {
            DrawStringBaseline(fontName, size, str, vec.X, vec.Y, vec.X, color);
        }

		public void DrawStringBaseline(string fontName, float size, string text, float x, float y, float start_x, Color4 color, bool underline = false, TextShadow shadow = default)
		{
			if (!active)
				throw new InvalidOperationException("Renderer2D.Start() must be called before Renderer2D.DrawString");
			if (text == "" || size < 1) //skip empty str
				return;
            CreateRichTextEngine().DrawStringBaseline(fontName, size, text, x, y, start_x, color, underline, shadow);
        }
        public void FillRectangle(Rectangle rect, Color4 color)
		{
			DrawQuad(dot, new Rectangle(0,0,1,1), rect, color, BlendMode.Normal);
		}

        const int C_TL = 0;
        private const int C_TR = 1 << 16;
        private const int C_BL = 1;
        private const int C_BR = 1 << 16 | 1;

       

        public void EllipseMask(Texture2D tex, Rectangle source, RectangleF parent, Vector2 center, Vector2 dimensions, float angle, Color4 color)
        {
            tex ??= dot;
            Prepare(BlendMode.Normal, tex, true);

            float x = center.X;
            float y = center.Y;
            float w = dimensions.X;
            float h = dimensions.Y;
            float dx = -dimensions.X / 2;
            float dy = -dimensions.Y / 2;
            
            float srcX = (float)source.X;
            float srcY = (float)source.Y;
            float srcW = (float)source.Width;
            float srcH = (float)source.Height;

            
            var cos = MathF.Cos(angle);
            var sin = MathF.Sin(angle);
            var tl = new Vector2(
                x + dx * cos - dy * sin,
                y + dx * sin + dy * cos
            );
            var tr = new Vector2(
                x+(dx+w)*cos-dy*sin,
                y+(dx+w)*sin+dy*cos
            );
            var bl = new Vector2(
                x + dx * cos - (dy + h) * sin,
                y + dx * sin + (dy + h) * cos
            );
            var br = new Vector2(
                x+(dx+w)*cos-(dy+h)*sin,
                y+(dx+w)*sin+(dy+h)*cos
                );

            Vector2 GetTexCoord(Vector2 x)
            {
                var rel = new Vector2(source.X, source.Y) +
                          (x - new Vector2(parent.X, parent.Y)) /
                          new Vector2(parent.Width, parent.Height) *
                          new Vector2(source.Width, source.Height);
                return rel / new Vector2(tex.Width, tex.Height);
            }
            
            vertices [vertexCount++] = new Vertex2D (
                tl, GetTexCoord(tl),
                C_TL,
                color
            );
            vertices [vertexCount++] = new Vertex2D (
                tr, GetTexCoord(tr),
                C_TR,
                color
            );
            vertices [vertexCount++] = new Vertex2D (
                bl, GetTexCoord(bl),
                C_BL,
                color
            );
            vertices [vertexCount++] = new Vertex2D (
                br, GetTexCoord(br),
                C_BR,
                color
            );

            primitiveCount += 2;
        }
        public void FillRectangleColors(RectangleF rec, Color4 tl, Color4 tr, Color4 bl, Color4 br)
        {
            Prepare(BlendMode.Normal, dot, false);

            float x = (float)rec.X;
            float y = (float)rec.Y;
            float w = (float)rec.Width;
            float h = (float)rec.Height;
            
            vertices [vertexCount++] = new Vertex2D (
                new Vector2 (x, y),
                Vector2.Zero,
                tl
            );
            vertices [vertexCount++] = new Vertex2D (
                new Vector2 (x + w, y),
                Vector2.Zero,
                tr
            );
            vertices [vertexCount++] = new Vertex2D (
                new Vector2(x, y + h),
                Vector2.Zero,
                bl
            );
            vertices [vertexCount++] = new Vertex2D (
                new Vector2 (x + w, y + h),
                Vector2.Zero,
                br
            );

            primitiveCount += 2;
        }
        void Prepare(BlendMode mode, Texture2D tex, bool circle)
        {
            if (currentMode != mode ||
                isCircle != circle ||
                (currentTexture != null && currentTexture != tex) ||
                (primitiveCount + 2) * 3 >= MAX_INDEX ||
                (vertexCount + 4) >= MAX_VERT)
            {
                Flush();
            }
            currentTexture = tex;
            currentMode = mode;
            isCircle = circle;
        }

        public void DrawLine(Color4 color, Vector2 start, Vector2 end)
        {
            Prepare(BlendMode.Normal, dot, false);

			var edge = end - start;
			var angle = (float)Math.Atan2(edge.Y, edge.X);
			var sin = (float)Math.Sin(angle);
			var cos = (float)Math.Cos(angle);
			var x = start.X;
			var y = start.Y;
			var w = edge.Length();

			vertices[vertexCount++] = new Vertex2D(
				new Vector2(x,y),
				Vector2.Zero,
				color
			);
			vertices[vertexCount++] = new Vertex2D(
				new Vector2(x + w * cos, y + (w * sin)),
				Vector2.Zero,
				color
			);
			vertices[vertexCount++] = new Vertex2D(
				new Vector2(x - sin, y + cos),
				Vector2.Zero,
				color
			);
			vertices[vertexCount++] = new Vertex2D(
				new Vector2(x + w * cos - sin, y + w * sin + cos),
				Vector2.Zero,
				color
			);

			primitiveCount += 2;
		}
		public void DrawRectangle(Rectangle rect, Color4 color, int width)
		{
			FillRectangle(new Rectangle(rect.X, rect.Y, rect.Width, width), color);
			FillRectangle(new Rectangle(rect.X, rect.Y, width, rect.Height), color);
			FillRectangle(new Rectangle(rect.X, rect.Y + rect.Height - width, rect.Width, width), color);
			FillRectangle(new Rectangle(rect.X + rect.Width - width, rect.Y, width, rect.Height), color);
		}

        public void DrawImageStretched(Texture2D tex, Rectangle dest, Color4 color, bool flip = false)
		{
			DrawQuad (
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
			DrawQuad(tex, source, dest, color, mode, flip);
		}

		public void FillTriangle(Vector2 point1, Vector2 point2, Vector2 point3, Color4 color)
        {
            Prepare(BlendMode.Normal, dot, false);
          
			vertices[vertexCount++] = new Vertex2D(
				point1,
				Vector2.Zero,
				color
			);
			vertices[vertexCount++] = new Vertex2D(
				point2,
				Vector2.Zero,
				color
			);
			vertices[vertexCount++] = new Vertex2D(
				point3,
				Vector2.Zero,
				color
			);
			vertices[vertexCount++] = new Vertex2D(
				point3,
				Vector2.Zero,
				color
			);

			primitiveCount += 2;

		}

        public void DrawTriangle(Texture2D tex, Vector2 pa, Vector2 pb, Vector2 pc, Vector2 uva, Vector2 uvb,
            Vector2 uvc, Color4 color)
        {
            Prepare(BlendMode.Normal, tex, false);

            vertices[vertexCount++] = new Vertex2D(
                pa, uva, color
                );
            vertices[vertexCount++] = new Vertex2D(
                pb, uvb, color
            );
            vertices[vertexCount++] = new Vertex2D(
                pc, uvc, color
            );
            vertices[vertexCount++] = new Vertex2D(
                pc, uvc, color
            );
            primitiveCount += 2;
        }

        public void DrawVerticalGradient(Rectangle rect, Color4 top, Color4 bottom)
        {
            Prepare(BlendMode.Normal, dot, false);
            var x = (float) rect.X;
            var y = (float) rect.Y;
            var w = (float) rect.Width;
            var h = (float) rect.Height;
            vertices[vertexCount++] = new Vertex2D(
                new Vector2(x,y),
                Vector2.Zero,
                top
            );
            vertices[vertexCount++] = new Vertex2D(
                new Vector2(x + w, y),
                Vector2.Zero,
                top
            );
            vertices[vertexCount++] = new Vertex2D(
                new Vector2(x, y + h), 
                Vector2.Zero,
                bottom
            );
            vertices[vertexCount++] = new Vertex2D(
                new Vector2(x + w, y + h),
                Vector2.Zero,
                bottom
             );
            primitiveCount += 2;
        }

        void DrawQuad(Texture2D tex, Rectangle source, Rectangle dest, Color4 color, BlendMode mode, bool flip = false)
        {
            Prepare(mode, tex, false);

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
                throw new InvalidOperationException("TextRenderer.Start() must be called before TextRenderer.Finish()");
            Flush();
            active = false;
        }

        public void Flush()
		{
			if (vertexCount == 0 || primitiveCount == 0)
				return;
			rs.Cull = false;
			rs.BlendMode = currentMode;
			rs.DepthEnabled = false;
			currentTexture.BindTo (0);
            if(currentTexture.Format == SurfaceFormat.R8)
                imgShader.SetFloat(blendLocation, 1f);
            else
                imgShader.SetFloat(blendLocation, 0f);
            imgShader.SetInteger(circleLocation, isCircle ? 1 : 0);
            var verts = new Vertex2D[vertexCount];
            for (int i = 0; i < vertexCount; i++)
                verts[i] = vertices[i];
            vbo.EndStreaming(vertexCount);
			vbo.Draw (PrimitiveTypes.TriangleList, primitiveCount);
            vertices = (Vertex2D*)vbo.BeginStreaming();
            vertexCount = 0;
			primitiveCount = 0;
			currentTexture = null;
			rs.Cull = true;
		}

		public void Dispose()
		{
			el.Dispose ();
			vbo.Dispose ();
		}
	}
}
