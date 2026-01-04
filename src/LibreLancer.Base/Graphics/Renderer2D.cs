// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Runtime.InteropServices;
using LibreLancer.Graphics.Text;
using LibreLancer.Graphics.Vertices;

namespace LibreLancer.Graphics;

public struct TexSource
{
    public Rectangle? Rectangle;
    public Vector2 TL;
    public Vector2 TR;
    public Vector2 BL;
    public Vector2 BR;

    public TexSource(Vector2 tl, Vector2 tr, Vector2 bl, Vector2 br)
    {
        Rectangle = null;
        TL = tl;
        TR = tr;
        BL = bl;
        BR = br;
    }

    internal TexSource Normalize(Texture2D tex)
    {
        if (Rectangle != null)
        {
            var srcX = Rectangle.Value.X;
            var srcY = Rectangle.Value.Y;
            var srcW = Rectangle.Value.Width;
            var srcH = Rectangle.Value.Height;

            Vector2 ta = new Vector2(srcX / (float) tex.Width,
                srcY / (float) tex.Height);
            Vector2 tb = new Vector2((srcX + srcW) / (float) tex.Width,
                srcY / (float) tex.Height);
            Vector2 tc = new Vector2(srcX / (float) tex.Width,
                (srcY + srcH) / (float) tex.Height);
            Vector2 td = new Vector2((srcX + srcW) / (float) tex.Width,
                (srcY + srcH) / (float) tex.Height);

            return new TexSource(ta, tb, tc, td);
        }
        return this;
    }

    public static implicit operator TexSource(Rectangle r) =>
        new TexSource() { Rectangle = r };
}
public unsafe class Renderer2D : IDisposable
{
    private const int MAX_GLYPHS = 2048; //2048 rendered quads/drawcall
    private const int MAX_VERT = MAX_GLYPHS * 4;
    private const int MAX_INDEX = MAX_GLYPHS * 6;


    [StructLayout(LayoutKind.Sequential)]
    private struct Vertex2D : IVertexType {
        public Vector2 Position;
        public Vector2 TexCoord;
        public VertexDiffuse Color;

        public Vertex2D(Vector2 position, Vector2 texcoord, Color4 color)
        {
            Position = position;
            TexCoord = texcoord;
            Color = (VertexDiffuse)color;
        }

        public VertexDeclaration GetVertexDeclaration()
        {
            return new VertexDeclaration (
                sizeof(float) * 2 + sizeof(float) * 2 + sizeof(int),
                new VertexElement (VertexSlots.Position, 2, VertexElementType.Float, false, 0),
                new VertexElement (VertexSlots.Texture1, 2, VertexElementType.Float, false, sizeof(float) * 2),
                new VertexElement (VertexSlots.Color, 4, VertexElementType.UnsignedByte, true, sizeof(float) * 4)
            );
        }
    }

    private RenderContext rs;
    private VertexBuffer vbo;
    private ElementBuffer el;
    private Vertex2D* vertices;
    private Shader imgShader;
    private Texture2D dot;

    internal Renderer2D (RenderContext rstate)
    {
        rs = rstate;
        imgShader = ShaderBundle.FromResource<Renderer2D>(rstate, "Shader2D.bin").Get(0);
        vbo = new VertexBuffer (rstate, typeof(Vertex2D), MAX_VERT, true);
        el = new ElementBuffer (rstate, MAX_INDEX);
        var indices = new ushort[MAX_INDEX];
        //vertices = new Vertex2D[MAX_VERT];
        var iptr = 0;
        for (var i = 0; i < MAX_VERT; i += 4) {
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
        dot = new Texture2D (rstate, 1, 1, false, SurfaceFormat.R8);
        dot.SetData (new byte[] { 255 });
    }

    private BlurgEngine? richText;
    public RichTextEngine CreateRichTextEngine() => richText ??= new BlurgEngine(rs, this);

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

    private int vertexCount = 0;
    private int primitiveCount = 0;
    private Texture2D? currentTexture;
    private ushort currentMode = BlendMode.Normal;
    private int cVpW = 0, cVpH = 0;

    private void SetViewport(int vpW, int vpH)
    {
        if (vpW == cVpW && vpH == cVpH)
        {
            return;
        }

        cVpW = vpW;
        cVpH = vpH;
        var mat = Matrix4x4.CreateOrthographicOffCenter (0, vpW, vpH, 0, 0, 1);
        imgShader.SetUniformBlock(2, ref mat);
    }

    public void DrawString(string fontName, float size, string str, Vector2 vec, Color4 color)
    {
        DrawStringBaseline(fontName, size, str, vec.X, vec.Y, color);
    }

    public void DrawStringBaseline(string fontName, float size, string text, float x, float y,
        Color4 color, bool underline = false, OptionalColor shadow = default)
    {
        if (text == "" || size < 1) //skip empty str
            return;
        CreateRichTextEngine().DrawStringBaseline(fontName, size, text, x, y, color, underline, shadow);
    }

    public void DrawStringCached(ref CachedRenderString cache, string fontName, float size, string text,
        float x, float y, Color4 color, bool underline = false, OptionalColor shadow = default,
        TextAlignment alignment = TextAlignment.Left, float maxWidth = 0)
    {
        if (text == "" || size < 1) //skip empty str
            return;
        CreateRichTextEngine().DrawStringCached(ref cache, fontName, size, text, x, y, color, underline, shadow, alignment, maxWidth);
    }

    public Point MeasureStringCached(ref CachedRenderString cache, string fontName, float size, string text, bool underline = false, bool shadow = false, TextAlignment alignment = TextAlignment.Left, float maxWidth = 0)
    {
        if (text == "" || size < 1) //skip empty str
            return Point.Zero;
        return CreateRichTextEngine().MeasureStringCached(ref cache, fontName, size, maxWidth, text, underline, shadow, alignment);
    }

    public void FillRectangle(Rectangle rect, Color4 color)
    {
        DrawQuad(dot, new Rectangle(), rect, color, BlendMode.Normal);
    }

    public void DrawRotated(Texture2D tex, TexSource source, Rectangle dest, Vector2 origin, Color4 color, ushort mode, float angle, bool flip = false, QuadRotation orient = QuadRotation.None)
    {
        Prepare(mode, tex);
        float x = dest.X;
        float y = dest.Y;
        float w = dest.Width;
        float h = dest.Height;
        var dx = -origin.X;
        var dy = -origin.Y;

        source = source.Normalize(tex);


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

        Vector2 ta = source.TL;
        Vector2 tb = source.TR;
        Vector2 tc = source.BL;
        Vector2 td = source.BR;

        var topLeftCoord = ta;
        var topRightCoord = tb;
        var bottomLeftCoord = tc;
        var bottomRightCoord = td;

        if (orient == QuadRotation.Rotate90)
        {
            topLeftCoord = tc;
            topRightCoord = ta;
            bottomLeftCoord = td;
            bottomRightCoord = tb;
        }
        else if (orient == QuadRotation.Rotate180)
        {
            topLeftCoord = td;
            bottomLeftCoord = tb;
            topRightCoord = tc;
            bottomRightCoord = ta;
        }
        else if (orient == QuadRotation.Rotate270)
        {
            topLeftCoord = tb;
            topRightCoord = td;
            bottomLeftCoord = ta;
            bottomRightCoord = tc;
        }
        vertices [vertexCount++] = new Vertex2D (
            tl, topLeftCoord,
            color
        );
        vertices [vertexCount++] = new Vertex2D (
            tr, topRightCoord,
            color
        );
        vertices [vertexCount++] = new Vertex2D (
            bl, bottomLeftCoord,
            color
        );
        vertices [vertexCount++] = new Vertex2D (
            br, bottomRightCoord,
            color
        );

        primitiveCount += 2;
    }

    public void FillRectangleColors(RectangleF rec, Color4 tl, Color4 tr, Color4 bl, Color4 br)
    {
        Prepare(BlendMode.Normal, dot);

        var x = (float)rec.X;
        var y = (float)rec.Y;
        var w = (float)rec.Width;
        var h = (float)rec.Height;

        vertices [vertexCount++] = new Vertex2D (
            new Vector2 (x, y),
            noTex,
            tl
        );
        vertices [vertexCount++] = new Vertex2D (
            new Vector2 (x + w, y),
            noTex,
            tr
        );
        vertices [vertexCount++] = new Vertex2D (
            new Vector2(x, y + h),
            noTex,
            bl
        );
        vertices [vertexCount++] = new Vertex2D (
            new Vector2 (x + w, y + h),
            noTex,
            br
        );

        primitiveCount += 2;
    }

    private void Prepare(ushort mode, Texture2D tex)
    {
        if (currentMode != mode ||
            (currentTexture != null && (currentTexture != tex && currentTexture != dot) && tex != dot) ||
            (primitiveCount + 2) * 3 >= MAX_INDEX ||
            (vertexCount + 4) >= MAX_VERT)
        {
            Flush();
        }
        if(vertices == (Vertex2D*)0) {
            currentMode = BlendMode.Normal;
            currentTexture = null;
            vertices = (Vertex2D*)vbo.BeginStreaming();
        }
        if (tex == dot) {
            currentTexture ??= tex;
        }
        else {
            currentTexture = tex;
        }
        currentMode = mode;
    }

    private static readonly Vector2 noTex = new Vector2(-9999, -9999);

    public void DrawLine(Color4 color, Vector2 start, Vector2 end)
    {
        Prepare(BlendMode.Normal, dot);

        var edge = end - start;
        var angle = (float)Math.Atan2(edge.Y, edge.X);
        var sin = (float)Math.Sin(angle);
        var cos = (float)Math.Cos(angle);
        var x = start.X;
        var y = start.Y;
        var w = edge.Length();

        vertices[vertexCount++] = new Vertex2D(
            new Vector2(x,y),
            noTex,
            color
        );

        vertices[vertexCount++] = new Vertex2D(
            new Vector2(x + w * cos, y + (w * sin)),
            noTex,
            color
        );

        vertices[vertexCount++] = new Vertex2D(
            new Vector2(x - sin, y + cos),
            noTex,
            color
        );

        vertices[vertexCount++] = new Vertex2D(
            new Vector2(x + w * cos - sin, y + w * sin + cos),
            noTex,
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

    public void DrawImageStretched(Texture2D tex, Rectangle dest, Color4 color, bool flip = false, QuadRotation orient = QuadRotation.None)
    {
        DrawQuad (
            tex,
            new Rectangle (0, 0, tex.Width, tex.Height),
            dest,
            color,
            BlendMode.Normal,
            flip,
            orient
        );
    }

    private void Swap<T>(ref T a, ref T b)
    {
        (a, b) = (b, a);
    }

    public void Draw(Texture2D tex, TexSource source, Rectangle dest, Color4 color, ushort mode = BlendMode.Normal, bool flip = false, QuadRotation orient = QuadRotation.None)
    {
        DrawQuad(tex, source, dest, color, mode, flip, orient);
    }


    public void DrawTriangle(Texture2D tex, Vector2 pa, Vector2 pb, Vector2 pc, Vector2 uva, Vector2 uvb,
        Vector2 uvc, Color4 color)
    {
        Prepare(BlendMode.Normal, tex);

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
        if (rs.ScissorEnabled && !rect.Intersects(rs.ScissorRectangle)) return;
        Prepare(BlendMode.Normal, dot);
        var x = (float) rect.X;
        var y = (float) rect.Y;
        var w = (float) rect.Width;
        var h = (float) rect.Height;
        vertices[vertexCount++] = new Vertex2D(
            new Vector2(x,y),
            noTex,
            top
        );
        vertices[vertexCount++] = new Vertex2D(
            new Vector2(x + w, y),
            noTex,
            top
        );
        vertices[vertexCount++] = new Vertex2D(
            new Vector2(x, y + h),
            noTex,
            bottom
        );
        vertices[vertexCount++] = new Vertex2D(
            new Vector2(x + w, y + h),
            noTex,
            bottom
        );
        primitiveCount += 2;
    }

    private void DrawQuad(Texture2D tex, TexSource source, Rectangle dest, Color4 color, ushort mode, bool flip = false, QuadRotation orient = QuadRotation.None)
    {
        if (rs.ScissorEnabled && !dest.Intersects(rs.ScissorRectangle)) return;
        Prepare(mode, tex);

        var x = (float)dest.X;
        var y = (float)dest.Y;
        var w = (float)dest.Width;
        var h = (float)dest.Height;

        source = source.Normalize(tex);

        var p1 = new Vector2(x, y);
        var p2 = new Vector2(x + w, y);
        var p3 = new Vector2(x, y + h);
        var p4 = new Vector2(x + w, y + h);

        Vector2 topLeftCoord;
        Vector2 topRightCoord;
        Vector2 bottomLeftCoord;
        Vector2 bottomRightCoord;
        if (tex != dot)
        {
            Vector2 ta = source.TL;
            Vector2 tb = source.TR;
            Vector2 tc = source.BL;
            Vector2 td = source.BR;
            topLeftCoord = ta;
            topRightCoord = tb;
            bottomLeftCoord = tc;
            bottomRightCoord = td;

            if (orient == QuadRotation.Rotate90)
            {
                topLeftCoord = tc;
                topRightCoord = ta;
                bottomLeftCoord = td;
                bottomRightCoord = tb;
            }
            else if (orient == QuadRotation.Rotate180)
            {
                topLeftCoord = td;
                bottomLeftCoord = tb;
                topRightCoord = tc;
                bottomRightCoord = ta;
            }
            else if (orient == QuadRotation.Rotate270)
            {
                topLeftCoord = tb;
                topRightCoord = td;
                bottomLeftCoord = ta;
                bottomRightCoord = tc;
            }

            if (flip)
            {
                Swap(ref bottomLeftCoord, ref topLeftCoord);
                Swap(ref bottomRightCoord, ref topRightCoord);
            }
        }
        else
        {
            topLeftCoord = topRightCoord = bottomLeftCoord = bottomRightCoord = noTex;
        }

        vertices [vertexCount++] = new Vertex2D (
            p1,
            topLeftCoord,
            color
        );
        vertices [vertexCount++] = new Vertex2D (
            p2,
            topRightCoord,
            color
        );
        vertices [vertexCount++] = new Vertex2D (
            p3,
            bottomLeftCoord,
            color
        );
        vertices [vertexCount++] = new Vertex2D (
            p4,
            bottomRightCoord,
            color
        );

        primitiveCount += 2;
    }

    internal void Flush()
    {
        if (vertexCount == 0 || primitiveCount == 0)
            return;
        rs.ApplyRenderTarget();
        rs.ApplyViewport();
        SetViewport(rs.CurrentViewport.Width, rs.CurrentViewport.Height);
        rs.Set2DState(false, false);
        rs.ApplyScissor();
        rs.SetBlendMode(currentMode);
        currentTexture!.BindTo (7);
        var blend = currentTexture.Format != SurfaceFormat.R8 ? 1 : 0;
        imgShader.SetUniformBlock(3, ref blend);
        rs.Backend.ApplyShader(imgShader.Backing);
        vbo.EndStreaming(vertexCount);
        vbo.DrawNoApply (PrimitiveTypes.TriangleList, primitiveCount);
        vertices = (Vertex2D*)0;
        vertexCount = 0;
        primitiveCount = 0;
        currentTexture = null;
    }

    public void Dispose()
    {
        el.Dispose ();
        vbo.Dispose ();
    }
}
