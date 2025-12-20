using System;
using System.Numerics;
using LibreLancer;
using LibreLancer.Data.GameData.Archetypes;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Render;
using LibreLancer.Render.Cameras;
using LibreLancer.Render.Materials;
using LibreLancer.Resources;

namespace LancerEdit.GameContent.Popups;

public class SunImmediateRenderer : IDisposable
{
    private RenderTarget2D drawTarget;
    private SunSpineMaterial spineMaterial;
    private SunRadialMaterial centerMaterial;
    private SunRadialMaterial glowMaterial;

    VertexBillboardColor2[] vertices;
    private VertexBuffer vertexBuffer;
    private IVertexType vtype;

    private LookAtCamera cam = new LookAtCamera();

    public SunImmediateRenderer(ResourceManager resources)
    {
        vtype = new VertexBillboardColor2();
        spineMaterial = new SunSpineMaterial(resources);
        spineMaterial.SizeMultiplier = Vector2.One;
        centerMaterial = new SunRadialMaterial(resources);
        centerMaterial.Additive = true;
        centerMaterial.SizeMultiplier = Vector2.One;
        centerMaterial.OuterAlpha = 1;
        glowMaterial = new SunRadialMaterial(resources);
        glowMaterial.SizeMultiplier = Vector2.One;
        centerMaterial.OuterAlpha = 1;
    }

    void EnsureCapacity(RenderContext context, int count)
    {
        if (vertices == null || vertices.Length < count)
        {
            vertexBuffer?.Elements?.Dispose();
            vertexBuffer?.Dispose();
            vertices = new VertexBillboardColor2[count];
            vertexBuffer = new VertexBuffer(context, typeof(VertexBillboardColor2), count);
            var indices = new ushort[(count / 4) * 6];
            int iptr = 0;
            for (int i = 0; i < (count); i += 4)
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

            var eb = new ElementBuffer(context, indices.Length);
            eb.SetData(indices);
            vertexBuffer.SetElementBuffer(eb);
        }
    }

    public unsafe void Render(Sun sun, Color4 background, RenderContext render, Rectangle? viewport)
    {
        if (viewport != null && render.ScissorEnabled && !viewport.Value.Intersects(render.ScissorRectangle))
        {
            //Skip
            return;
        }

        Matrix4x4 world = Matrix4x4.Identity;
        WorldMatrixHandle handle = new WorldMatrixHandle()
        {
            ID = ulong.MaxValue, Source = &world
        };
        spineMaterial.World = handle;
        centerMaterial.World = handle;
        glowMaterial.World = handle;

        var restore = render.RenderTarget;

        if (viewport != null)
        {
            var vp = viewport.Value;
            render.PushViewport(new Rectangle(0, 0, vp.Width, vp.Height));
            render.PushScissor(new Rectangle(0, 0, vp.Width, vp.Height), false);
            if (drawTarget == null ||
                drawTarget?.Width != vp.Width || drawTarget?.Height != vp.Height)
            {
                drawTarget?.Dispose();
                drawTarget = new RenderTarget2D(render, vp.Width, vp.Height);
            }

            render.RenderTarget = drawTarget;
            render.ClearColor = background;
            render.ClearAll();

        }

        // Calculate size of sun
        float renderSize = sun.Radius;
        if (sun.SpinesSprite != null)
        {
            float rmax = sun.Radius;
            if (renderSize * sun.SpinesScale > renderSize)
                rmax = sun.Radius * sun.SpinesScale;
            foreach (var s in sun.Spines)
            {
                var multMax = MathF.Max(s.WidthScale / s.LengthScale, s.LengthScale);
                if (sun.Radius * sun.SpinesScale * multMax > rmax)
                    rmax = sun.Radius * sun.SpinesScale * multMax;
            }

            renderSize = rmax;
        }

        //camera
        cam.Update(render.CurrentViewport.Width, render.CurrentViewport.Height, new Vector3(0, 0, -renderSize * 0.9f),
            Vector3.Zero);
        render.SetCamera(cam);
        render.Cull = false;
        render.DepthEnabled = false;

        var count = SunRenderer.GetVertexCount(sun);
        EnsureCapacity(render, count);
        SunRenderer.CreateVertices(vertices, Vector3.Zero, sun);
        vertexBuffer.SetData<VertexBillboardColor2>(vertices.AsSpan().Slice(0, count));
        spineMaterial.Texture = sun.SpinesSprite;
        centerMaterial.Texture = sun.CenterSprite;
        glowMaterial.Texture = sun.GlowSprite;
        int idx = 0;
        if (sun.CenterSprite != null)
        {
            centerMaterial.Use(render, vtype, ref Lighting.Empty, 0);
            vertexBuffer.Draw(PrimitiveTypes.TriangleList, 0, idx, 2);
            idx += 6;
        }

        glowMaterial.Use(render, vtype, ref Lighting.Empty, 0);
        vertexBuffer.Draw(PrimitiveTypes.TriangleList, 0, idx, 2);
        idx += 6;
        if (sun.Spines != null)
        {
            spineMaterial.Use(render, vtype, ref Lighting.Empty, 0);
            vertexBuffer.Draw(PrimitiveTypes.TriangleList, 0, idx, sun.Spines.Count * 2);
        }

        if (viewport != null)
        {
            render.PopScissor();
            render.PopViewport();
            render.RenderTarget = restore;
            var vp = viewport.Value;
            if (restore != null)
                drawTarget.BlitToBuffer((RenderTarget2D)restore, new Point(vp.X, vp.Y));
            else
                drawTarget.BlitToScreen(new Point(vp.X, vp.Y));
        }
    }

    public void Dispose()
    {
        vertexBuffer?.Elements?.Dispose();
        vertexBuffer?.Dispose();
        drawTarget?.Dispose();
    }
}
