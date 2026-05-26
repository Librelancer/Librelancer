using System;
using System.Collections.Generic;
using System.Numerics;
using BepuUtilities.Memory;
using LibreLancer.Graphics.Text;

namespace LibreLancer.Graphics;

public class DrawList2D
{
    private const int MinVertexSize = 4 * (256); // 256 quads
    private const int MinIndexSize = 6 * (256);

    private static readonly Vector2 noTex = new Vector2(-9999, -9999);

    record struct DrawCall(
        int BaseVertex,
        int StartIndex,
        int PrimitiveCount,
        ushort BlendMode,
        Texture? Texture,
        Rectangle? Clip,
        Action<RenderContext>? Callback);

    private List<DrawCall> drawCalls = new();
    private Stack<Rectangle> clips = new Stack<Rectangle>();

    private int baseVertex = 0;
    private int vertexCount = 0;
    private int startIndex = 0;
    private int indexCount = 0;

    private Texture? currentTexture;

    private Texture2D dot;
    private ushort currentMode = BlendMode.Normal;

    private Rectangle? activeClip = null;

    private BufferPool pool;
    private Buffer<Vertex2D> vertices;
    private Buffer<ushort> indices;
    private Renderer2D ren;

    internal DrawList2D(Renderer2D ren, Texture2D dot, BufferPool pool)
    {
        this.dot = dot;
        this.pool = pool;
        pool.TakeAtLeast(MinVertexSize, out vertices);
        pool.TakeAtLeast(MinIndexSize, out indices);
        this.ren = ren;
    }

    void AllocateGeometry(int vertex, int index)
    {
        if (vertexCount + vertex > vertices.Length)
        {
            var targetSize = Math.Max((int)(vertices.Length * 1.5), vertexCount + vertex);
            pool.ResizeToAtLeast(ref vertices, targetSize, vertexCount);
        }


        if (indexCount + index > indices.Length)
        {
            var targetSize = Math.Max((int)(indices.Length * 1.5), indexCount + index);
            pool.ResizeToAtLeast(ref indices, targetSize, indexCount);
        }
    }

    void Quad()
    {
        var v0 = (ushort)(vertexCount - baseVertex);
        var v1 = (ushort)(v0 + 1);
        var v2 = (ushort)(v0 + 2);
        var v3 = (ushort)(v0 + 3);
        indices[indexCount] = v0;
        indices[indexCount + 1] = v1;
        indices[indexCount + 2] = v2;
        indices[indexCount + 3] = v1;
        indices[indexCount + 4] = v3;
        indices[indexCount + 5] = v2;
        indexCount += 6;
    }

    void Triangle()
    {
        var v0 = (ushort)(vertexCount - baseVertex);
        var v1 = (ushort)(v0 + 1);
        var v2 = (ushort)(v0 + 2);
        indices[indexCount] = v0;
        indices[indexCount + 1] = v1;
        indices[indexCount + 2] = v2;
        indexCount += 3;
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
        var primCount = (indexCount - startIndex) / 3;
        if (primCount > 0)
        {
            drawCalls.Add(new(baseVertex, startIndex, primCount, currentMode, currentTexture, activeClip, null));
        }
        currentMode = BlendMode.Normal;
        currentTexture = null;
        startIndex = indexCount;
        baseVertex = vertexCount;
    }

    public void AddCallback(Action<RenderContext> callback)
    {
        Flush();
        drawCalls.Add(new(0, 0, 0, 0, null, activeClip, callback));
    }

    public unsafe void Render(bool parentClip = true)
    {
        Flush();
        Renderer2D.VertexAllocation vbo = default;
        if (vertexCount > 0)
        {
            vbo = ren.UploadGeometry(vertices.Memory, vertexCount, indices.Memory, indexCount);
        }
        pool.Return(ref vertices);
        pool.Return(ref indices);
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
            vbo.VertexBuffer?.Draw(PrimitiveTypes.TriangleList, call.BaseVertex, call.StartIndex, call.PrimitiveCount);
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

    private void Prepare(ushort mode, Texture2D tex, int vertices)
    {
        var vtx = (vertexCount - baseVertex);
        if (currentMode != mode ||
            (currentTexture != null && (currentTexture != tex && currentTexture != dot) && tex != dot) ||
            vtx + vertices >= ushort.MaxValue)
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

    public void DrawLine(Color4 color, Vector2 start, Vector2 end, float thickness = 1.0f)
    {
        Span<Vector2> points = stackalloc Vector2[2];
        points[0] = start;
        points[1] = end;
        DrawPolyline(points, (VertexDiffuse)color, thickness);
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
        Prepare(BlendMode.Normal, tex, 4);
        AllocateGeometry(3, 3);
        Triangle();
        vertices[vertexCount++] = new Vertex2D(
            pa, uva, color
        );
        vertices[vertexCount++] = new Vertex2D(
            pb, uvb, color
        );
        vertices[vertexCount++] = new Vertex2D(
            pc, uvc, color
        );
    }

    // Port of imgui_draw.cpp AddPolyline to our render structure.
    const float AAFringeScale = 1.0f;

    private const float IM_FIXNORMAL2F_MAX_INVLEN2 = 100f;
    static Vector2 FixNormal(Vector2 n)
    {
        var d2 = n.LengthSquared();
        if (d2 > 0.000001f)
        {
            float inv_len2 = 1f / d2;
            return n * Math.Min(inv_len2, IM_FIXNORMAL2F_MAX_INVLEN2);
        }
        return n;
    }

    static Buffer<T> At<T>(Buffer<T> buffer, int index) where T : unmanaged
    {
        return buffer.Slice(index, buffer.Length - index);
    }

    public void DrawPolyline(ReadOnlySpan<Vector2> points, VertexDiffuse color,
       float thickness = 1f, bool closed = false)
    {
        if (color.A == 0 || points.Length < 2)
            return;

        VertexDiffuse col_trans = color with { A = 0 };
        // nThe number of line segments we need to draw
        int count = closed ? points.Length : points.Length - 1;

        bool thick_line = thickness > AAFringeScale;


        // Thicknesses <1.0 should behave like thickness 1.0
        thickness = Math.Max(thickness, 1f);

        int idxCount = thick_line ? count * 18 : count * 12;
        int vtxCount = thick_line ? points.Length * 4 : points.Length * 3;

        Prepare(BlendMode.Normal, dot, vtxCount);
        AllocateGeometry(vtxCount, idxCount);

        pool.TakeAtLeast(points.Length * (thick_line ? 5 : 3), out Buffer<Vector2> tempBuffer);

        var tempNormals = tempBuffer.Slice(0, points.Length);
        var tempPoints = At(tempBuffer, points.Length);

        // Calculate normals (tangents) for each line segment
        for (int i1 = 0; i1 < count; i1++)
        {
            var i2 = (i1 + 1) == points.Length ? 0 : i1 + 1;

            var d = points[i2] - points[i1];
            if (d.LengthSquared() > 0)
            {
                d = Vector2.Normalize(d);
            }
            tempNormals[i1] = new(d.Y, -d.X);
        }

        if (!closed)
            tempNormals[points.Length - 1] = tempNormals[points.Length - 2];

        if (thick_line)
        {
            // [PATH 2] Non texture-based lines (thick): we need to draw the solid line core and thus require four vertices per point
            float half_inner_thickness = (thickness - AAFringeScale) * 0.5f;

            // If line is not closed, the first and last points need to be generated differently as there are no normals to blend
            if (!closed)
            {
                int points_last = points.Length - 1;
                tempPoints[0] = points[0] + tempNormals[0] * (half_inner_thickness + AAFringeScale);
                tempPoints[1] = points[0] + tempNormals[0] * (half_inner_thickness);
                tempPoints[2] = points[0] - tempNormals[0] * (half_inner_thickness);
                tempPoints[3] = points[0] - tempNormals[0] * (half_inner_thickness + AAFringeScale);
                tempPoints[points_last * 4 + 0] = points[points_last] + tempNormals[points_last] * (half_inner_thickness + AAFringeScale);
                tempPoints[points_last * 4 + 1] = points[points_last] + tempNormals[points_last] * (half_inner_thickness);
                tempPoints[points_last * 4 + 2] = points[points_last] - tempNormals[points_last] * (half_inner_thickness);
                tempPoints[points_last * 4 + 3] = points[points_last] - tempNormals[points_last] * (half_inner_thickness + AAFringeScale);
            }

            // Generate the indices to form a number of triangles for each line segment, and the vertices for the line edges
            // This takes points n and n+1 and writes into n+1, with the first point in a closed line being generated from the final one (as n+1 wraps)
            // FIXME-OPT: Merge the different loops, possibly remove the temporary buffer.
            int idx1 = (vertexCount - baseVertex); // Vertex index for start of line segment
            for (int i1 = 0; i1 < count; i1++) // i1 is the first point of the line segment
            {
                int i2 = (i1 + 1) == points.Length ? 0 : (i1 + 1); // i2 is the second point of the line segment
                int idx2 = (i1 + 1) == points.Length ? idx1 : (idx1 + 4); // Vertex index for end of segment

                // Average normals
                var dm = FixNormal((tempNormals[i1] + tempNormals[i2]) * 0.5f);
                var dmOut = dm * (half_inner_thickness + AAFringeScale);
                var dmIn = dm * half_inner_thickness;

                // Add temporary vertices
                Buffer<Vector2> out_vtx = At(tempPoints, i2 * 4);
                out_vtx[0] = points[i2] + dmOut;
                out_vtx[1] = points[i2] + dmIn;
                out_vtx[2] = points[i2] - dmIn;
                out_vtx[3] = points[i2] - dmOut;

                // Add indexes
                Buffer<ushort> _IdxWritePtr = At(indices, indexCount);
                _IdxWritePtr[0]  = (ushort)(idx2 + 1); _IdxWritePtr[1]  = (ushort)(idx1 + 1); _IdxWritePtr[2]  = (ushort)(idx1 + 2);
                _IdxWritePtr[3]  = (ushort)(idx1 + 2); _IdxWritePtr[4]  = (ushort)(idx2 + 2); _IdxWritePtr[5]  = (ushort)(idx2 + 1);
                _IdxWritePtr[6]  = (ushort)(idx2 + 1); _IdxWritePtr[7]  = (ushort)(idx1 + 1); _IdxWritePtr[8]  = (ushort)(idx1 + 0);
                _IdxWritePtr[9]  = (ushort)(idx1 + 0); _IdxWritePtr[10] = (ushort)(idx2 + 0); _IdxWritePtr[11] = (ushort)(idx2 + 1);
                _IdxWritePtr[12] = (ushort)(idx2 + 2); _IdxWritePtr[13] = (ushort)(idx1 + 2); _IdxWritePtr[14] = (ushort)(idx1 + 3);
                _IdxWritePtr[15] = (ushort)(idx1 + 3); _IdxWritePtr[16] = (ushort)(idx2 + 3); _IdxWritePtr[17] = (ushort)(idx2 + 2);
                indexCount += 18;

                idx1 = idx2;
            }

            // Add vertices
            for (int i = 0; i < points.Length; i++)
            {
                vertices[vertexCount++] = new Vertex2D(tempPoints[i * 4 + 0], noTex, col_trans);
                vertices[vertexCount++] = new Vertex2D(tempPoints[i * 4 + 1], noTex, color);
                vertices[vertexCount++] = new Vertex2D(tempPoints[i * 4 + 2], noTex, color);
                vertices[vertexCount++] = new Vertex2D(tempPoints[i * 4 + 3], noTex, col_trans);
            }
        }
        else
        {
            // [PATH 2] Non texture-based lines (non-thick)
            // The width of the geometry we need to draw - this is essentially <thickness> pixels for the line itself, plus "one pixel" for AA.
            // - In the texture-based path, we don't use AAFringeScale here because the +1 is tied to the generated texture
            //   (see ImFontAtlasBuildRenderLinesTexData() function), and so alternate values won't work without changes to that code.
            // - In the non texture-based paths, we would allow AAFringeScale to potentially be != 1.0f with a patch (e.g. fringe_scale patch to
            //   allow scaling geometry while preserving one-screen-pixel AA fringe).
            float half_draw_size = AAFringeScale;

            // If line is not closed, the first and last points need to be generated differently as there are no normals to blend
            if (!closed)
            {
                tempPoints[0] = points[0] + tempNormals[0] * half_draw_size;
                tempPoints[1] = points[0] - tempNormals[0] * half_draw_size;
                tempPoints[(points.Length-1)*2+0] = points[points.Length-1] + tempNormals[points.Length-1] * half_draw_size;
                tempPoints[(points.Length-1)*2+1] = points[points.Length-1] - tempNormals[points.Length-1] * half_draw_size;
            }

            // Generate the indices to form a number of triangles for each line segment, and the vertices for the line edges
            // This takes points n and n+1 and writes into n+1, with the first point in a closed line being generated from the final one (as n+1 wraps)
            // FIXME-OPT: Merge the different loops, possibly remove the temporary buffer.

            int idx1 = (vertexCount - baseVertex); // Vertex index for start of line segment
            for (int i1 = 0; i1 < count; i1++) // i1 is the first point of the line segment
            {
                int i2 = (i1 + 1) == points.Length ? 0 : (i1 + 1); // i2 is the second point of the line segment
                int idx2 = (i1 + 1) == points.Length ? idx1 : (idx1 + 3); // Vertex index for end of segment

                // Average normals
                var dm = FixNormal((tempNormals[i1] + tempNormals[i2]) * 0.5f);
                dm *= half_draw_size; // dm_x, dm_y are offset to the outer edge of the AA area

                Buffer<Vector2> out_vtx = At(tempPoints, i2 * 2);
                out_vtx[0] = points[i2] + dm;
                out_vtx[1] = points[i2] - dm;

                Buffer<ushort> _IdxWritePtr = At(indices, indexCount);
                _IdxWritePtr[0] = (ushort)(idx2 + 0); _IdxWritePtr[1] = (ushort)(idx1 + 0); _IdxWritePtr[2] = (ushort)(idx1 + 2); // Right tri 1
                _IdxWritePtr[3] = (ushort)(idx1 + 2); _IdxWritePtr[4] = (ushort)(idx2 + 2); _IdxWritePtr[5] = (ushort)(idx2 + 0); // Right tri 2
                _IdxWritePtr[6] = (ushort)(idx2 + 1); _IdxWritePtr[7] = (ushort)(idx1 + 1); _IdxWritePtr[8] = (ushort)(idx1 + 0); // Left tri 1
                _IdxWritePtr[9] = (ushort)(idx1 + 0); _IdxWritePtr[10] = (ushort)(idx2 + 0); _IdxWritePtr[11] = (ushort)(idx2 + 1); // Left tri 2
                indexCount += 12;

                idx1 = idx2;
            }

            for (int i = 0; i < points.Length; i++)
            {
                // Center of line
                vertices[vertexCount++] = new(points[i], noTex, color);
                // Left-side outer edge
                vertices[vertexCount++] = new(tempPoints[i * 2 + 0], noTex, col_trans);
                // Right-side outer edge
                vertices[vertexCount++] = new(tempPoints[i * 2 + 1], noTex, color);
            }
        }
    }


    public void DrawVerticalGradient(Rectangle rect, Color4 top, Color4 bottom)
    {
        if (activeClip != null && !rect.Intersects(activeClip.Value)) return;
        Prepare(BlendMode.Normal, dot, 4);
        AllocateGeometry(4, 6);
        Quad();
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
    }

    public void FillRectangle(Rectangle rect, Color4 color)
    {
        DrawQuad(dot, new Rectangle(), rect, color, BlendMode.Normal);
    }

    public void DrawRotated(Texture2D tex, TexSource source, Rectangle dest, Vector2 origin, Color4 color, ushort mode, float angle, bool flip = false, QuadRotation orient = QuadRotation.None)
    {
        Prepare(mode, tex, 4);
        AllocateGeometry(4, 6);
        Quad();
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
    }

    public void FillRectangleColors(RectangleF rec, Color4 tl, Color4 tr, Color4 bl, Color4 br)
    {
        Prepare(BlendMode.Normal, dot, 4);
        AllocateGeometry(4, 6);
        Quad();

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
    }

    private void DrawQuad(Texture2D tex, TexSource source, Rectangle dest, Color4 color, ushort mode, bool flip = false, QuadRotation orient = QuadRotation.None)
    {
        if (activeClip != null && !dest.Intersects(activeClip.Value)) return;
        Prepare(mode, tex, 4);
        AllocateGeometry(4, 6);
        Quad();

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
    }
}
