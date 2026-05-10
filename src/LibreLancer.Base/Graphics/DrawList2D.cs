using System;
using System.Collections.Generic;
using System.Numerics;
using BepuUtilities.Memory;
using LibreLancer.Graphics.Text;

namespace LibreLancer.Graphics;

public class DrawList2D
{
    private const int MinSize = 4 * (256); // 256 quads
    private static readonly Vector2 noTex = new Vector2(-9999, -9999);

    record struct DrawCall(
        int BaseVertex,
        int PrimitiveCount,
        ushort BlendMode,
        Texture? Texture,
        Rectangle? Clip,
        Action<RenderContext>? Callback);

    private List<DrawCall> drawCalls = new();
    private Stack<Rectangle> clips = new Stack<Rectangle>();

    private int baseVertex = 0;
    private int vertexCount = 0;
    private int primitiveCount = 0;
    private Texture? currentTexture;

    private Texture2D dot;
    private ushort currentMode = BlendMode.Normal;

    private Rectangle? activeClip = null;

    private BufferPool pool;
    private Buffer<Vertex2D> vertices;
    private Renderer2D ren;

    internal DrawList2D(Renderer2D ren, Texture2D dot, BufferPool pool)
    {
        this.dot = dot;
        this.pool = pool;
        pool.TakeAtLeast(MinSize, out vertices);
        this.ren = ren;
    }

    void Allocate(int count)
    {
        if (vertexCount + count > vertices.Length)
        {
            pool.ResizeToAtLeast(ref vertices, (int)(vertices.Length * 1.5), vertexCount);
        }
    }

    public bool PushClip(Rectangle clip, bool parentClip = true)
    {
        Flush();
        if(clips.Count > 0 && parentClip) {
            if (!Rectangle.Clip(clip, clips.Peek(), out var newRect))
            {
                return false;
            }
            activeClip = newRect;
            clips.Push(newRect);
        }
        else
        {
            activeClip = clip;
            clips.Push(clip);
        }
        return true;
    }

    public bool SetClip(Rectangle newRect, bool parentClip = true)
    {
        Flush();
        if (clips.Count > 0)
            clips.Pop();
        return PushClip(newRect, parentClip);
    }

    public void PopClip()
    {
        Flush();
        if (clips.Count == 0)
            throw new InvalidOperationException();
        clips.Pop();
        activeClip = clips.Count > 0 ? clips.Peek() : null;
    }

    void Flush()
    {
        if (primitiveCount > 0)
            drawCalls.Add(new(baseVertex, primitiveCount, currentMode, currentTexture, activeClip, null));
        currentMode = BlendMode.Normal;
        currentTexture = null;
        baseVertex = vertexCount;
        primitiveCount = 0;
    }

    public void AddCallback(Action<RenderContext> callback)
    {
        Flush();
        drawCalls.Add(new(0, 0, 0, null, activeClip, callback));
    }

    public unsafe void Render(bool parentClip = true)
    {
        Flush();
        Renderer2D.VertexAllocation vbo = default;
        if (vertexCount > 0)
        {
            vbo = ren.UploadVertices(vertices.Memory, vertexCount);
        }
        pool.Return(ref vertices);
        var rc = ren.RenderContext;
        rc.Cull = false;
        rc.DepthEnabled = false;
        foreach (var call in drawCalls)
        {
            if (call.Clip != null)
            {
                if (!rc.PushScissor(call.Clip.Value, parentClip))
                    continue;
            }

            if (call.Callback != null)
            {
                call.Callback(rc);
                if (call.Clip != null)
                {
                    rc.PopScissor();
                }
                continue;
            }

            ren.SetViewport(rc.CurrentViewport.Width, rc.CurrentViewport.Height);

            var tex = call.Texture ?? ren.Dot;

            rc.Textures[0] = tex;
            rc.Samplers[0] = new(rc.PreferredFilterLevel, WrapMode.Repeat, WrapMode.Repeat);
            rc.Shader = ren.ImgShader;
            rc.BlendMode = call.BlendMode;
            var swizzle = tex.Format != SurfaceFormat.R8 ? 1 : 0;
            ren.ImgShader.SetUniformBlock(3, ref swizzle);
            vbo.VertexBuffer?.Draw(PrimitiveTypes.TriangleList, call.BaseVertex, 0, call.PrimitiveCount);
            if (call.Clip != null)
            {
                rc.PopScissor();
            }
        }
        if (vertexCount > 0)
        {
            ren.ReturnAllocation(vbo);
        }
    }

    private void Prepare(ushort mode, Texture2D tex)
    {
        if (currentMode != mode ||
            (currentTexture != null && (currentTexture != tex && currentTexture != dot) && tex != dot) ||
            (primitiveCount + 2) * 3 >= ushort.MaxValue)
        {
            Flush();
        }

        currentMode = mode;
        if (tex == dot) {
            currentTexture ??= tex;
        }
        else {
            currentTexture = tex;
        }
    }

    private void Swap<T>(ref T a, ref T b)
    {
        (a, b) = (b, a);
    }

    public void DrawStringBaseline(string fontName, float size, string text, Vector2 pos,
        Color4 color, bool underline = false, OptionalColor shadow = default) =>
        DrawStringBaseline(fontName, size, text, pos.X, pos.Y, color, underline, shadow);

    public void DrawStringBaseline(string fontName, float size, string text, float x, float y,
        Color4 color, bool underline = false, OptionalColor shadow = default)
    {
        if (text == "" || size < 1) //skip empty str
            return;
        ren.RichText.DrawStringBaseline(this, fontName, size, text, x, y, color, underline, shadow);
    }

    public void DrawStringCached(ref CachedRenderString? cache, string fontName, float size, string text,
        float x, float y, Color4 color, bool underline = false, OptionalColor shadow = default,
        TextAlignment alignment = TextAlignment.Left, float maxWidth = 0)
    {
        if (text == "" || size < 1) //skip empty str
            return;
        ren.RichText.DrawStringCached(this, ref cache, fontName, size, text, x, y, color, underline, shadow,
            alignment, maxWidth);
    }

    public void Draw(Texture2D tex, TexSource source, Rectangle dest, Color4 color, ushort mode = BlendMode.Normal, bool flip = false, QuadRotation orient = QuadRotation.None)
    {
        DrawQuad(tex, source, dest, color, mode, flip, orient);
    }

    public void DrawLine(Color4 color, Vector2 start, Vector2 end)
    {
        Prepare(BlendMode.Normal, dot);
        Allocate(4);
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


    public void DrawTriangle(Texture2D tex, Vector2 pa, Vector2 pb, Vector2 pc, Vector2 uva, Vector2 uvb,
        Vector2 uvc, Color4 color)
    {
        Prepare(BlendMode.Normal, tex);
        Allocate(4);
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
        if (activeClip != null && !rect.Intersects(activeClip.Value)) return;
        Prepare(BlendMode.Normal, dot);
        Allocate(4);
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

    public void FillRectangle(Rectangle rect, Color4 color)
    {
        DrawQuad(dot, new Rectangle(), rect, color, BlendMode.Normal);
    }

    public void DrawRotated(Texture2D tex, TexSource source, Rectangle dest, Vector2 origin, Color4 color, ushort mode, float angle, bool flip = false, QuadRotation orient = QuadRotation.None)
    {
        Prepare(mode, tex);
        Allocate(4);
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
        Allocate(4);

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

    private void DrawQuad(Texture2D tex, TexSource source, Rectangle dest, Color4 color, ushort mode, bool flip = false, QuadRotation orient = QuadRotation.None)
    {
        if (activeClip != null && !dest.Intersects(activeClip.Value)) return;
        Prepare(mode, tex);
        Allocate(4);

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
}
