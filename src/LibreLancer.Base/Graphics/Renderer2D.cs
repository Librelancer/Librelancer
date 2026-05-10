// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using BepuUtilities.Memory;
using LibreLancer.Graphics.Text;

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

            Vector2 ta = new Vector2(srcX / (float)tex.Width,
                srcY / (float)tex.Height);
            Vector2 tb = new Vector2((srcX + srcW) / (float)tex.Width,
                srcY / (float)tex.Height);
            Vector2 tc = new Vector2(srcX / (float)tex.Width,
                (srcY + srcH) / (float)tex.Height);
            Vector2 td = new Vector2((srcX + srcW) / (float)tex.Width,
                (srcY + srcH) / (float)tex.Height);

            return new TexSource(ta, tb, tc, td);
        }

        return this;
    }

    public static implicit operator TexSource(Rectangle r) =>
        new TexSource() { Rectangle = r };
}

public unsafe class Renderer2D : IDisposable
{
    private const int MaxQuadsPerDraw = 65535 / 6;

    internal RenderContext RenderContext;
    internal Shader ImgShader;
    internal Texture2D Dot;
    private Vertex2D* vertices;
    private BufferPool pool;

    private VertexBuffer?[] vbos = new VertexBuffer[8];
    private ElementBuffer?[] ebos = new ElementBuffer[8];
    private int[] counts = new int[8];
    private bool[] used = new bool[8];
    private int cVpW = 0, cVpH = 0;


    internal record struct VertexAllocation(int Index, VertexBuffer? VertexBuffer);

    public RichTextEngine RichText { get; private set; }


    internal Renderer2D(RenderContext rstate)
    {
        RenderContext = rstate;
        ImgShader = ShaderBundle.FromResource<Renderer2D>(rstate, "Shader2D.bin").Get(0);
        Dot = new Texture2D(rstate, 1, 1, false, SurfaceFormat.R8);
        Dot.SetData(new byte[] { 255 });
        pool = new();
        RichText = new RichTextEngine(RenderContext, this);
    }


    public Point MeasureString(string fontName, float size, string str)
    {
        if (str == "" || size < 1) //skip empty str
            return new Point(0, 0);
        return RichText.MeasureString(fontName, size, str);
    }

    public float LineHeight(string fontName, float size)
    {
        if (size < 1) return 0;
        return RichText.LineHeight(fontName, size);
    }

    public Point MeasureStringCached(ref CachedRenderString? cache, string fontName, float size, string text,
        bool underline = false, bool shadow = false, TextAlignment alignment = TextAlignment.Left, float maxWidth = 0)
    {
        if (text == "" || size < 1) //skip empty str
            return Point.Zero;
        return RichText.MeasureStringCached(ref cache, fontName, size, maxWidth, text, underline, shadow, alignment);
    }

    public DrawList2D CreateDrawList() => new(this, Dot, pool);


    internal VertexAllocation UploadVertices(Vertex2D* pointer, int count)
    {
        if (count == 0)
        {
            return default;
        }

        int index = -1;
        for (int i = 0; i < counts.Length; i++)
        {
            if(!used[i])
            {
                index = i;
                break;
            }
        }

        used[index] = true;
        // Recreate vertex buffer if needed
        if (count > counts[index])
        {
            vbos[index]?.Dispose();
            ebos[index]?.Dispose();
            int totalQuads = (count + 4) / 4;
            vbos[index] = new VertexBuffer(RenderContext, typeof(Vertex2D), totalQuads * 4, true);
            counts[index] = totalQuads * 4;
            int elemQuads = Math.Min(totalQuads, MaxQuadsPerDraw);

            ebos[index] = new ElementBuffer(RenderContext, elemQuads * 6);
            var indices = new ushort[elemQuads * 6];
            var iptr = 0;
            for (var i = 0; i < elemQuads * 4; i += 4)
            {
                /* Triangle 1 */
                indices[iptr++] = (ushort)i;
                indices[iptr++] = (ushort)(i + 1);
                indices[iptr++] = (ushort)(i + 2);
                /* Triangle 2 */
                indices[iptr++] = (ushort)(i + 1);
                indices[iptr++] = (ushort)(i + 3);
                indices[iptr++] = (ushort)(i + 2);
            }

            ebos[index]!.SetData(indices);
            vbos[index]!.SetElementBuffer(ebos[index]!);
        }

        var tgt = vbos[index]!.BeginStreaming();
        Buffer.MemoryCopy(
            (void*)pointer, (void*)tgt,
            counts[index] * Vertex2D.Size,
            count * Vertex2D.Size);
        vbos[index]!.EndStreaming(count);
        return new(index, vbos[index]);
    }

    internal void ReturnAllocation(VertexAllocation allocation)
    {
        used[allocation.Index] = false;
    }


    internal void SetViewport(int vpW, int vpH)
    {
        if (vpW == cVpW && vpH == cVpH)
        {
            return;
        }

        cVpW = vpW;
        cVpH = vpH;
        var mat = Matrix4x4.CreateOrthographicOffCenter(0, vpW, vpH, 0, 0, 1);
        ImgShader.SetUniformBlock(2, ref mat);
    }

    public void Dispose()
    {
        for (int i = 0; i < vbos.Length; i++)
        {
            vbos[i]?.Dispose();
            ebos[i]?.Dispose();
            counts[i] = 0;
        }
        pool.AssertEmpty();
    }
}
