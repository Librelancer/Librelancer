using System;
using System.Numerics;
using LibreLancer;
using LibreLancer.Data;
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
    private readonly ResourceManager resources;

    VertexBillboardColor2[] vertices;
    private VertexBuffer vertexBuffer;
    private IVertexType vtype;

    private LookAtCamera cam = new LookAtCamera();

    public SunImmediateRenderer(ResourceManager resources)
    {
        this.resources = resources;
        vtype = new VertexBillboardColor2();
        spineMaterial = new SunSpineMaterial(resources, null, Vector2.One);

        centerMaterial = new SunRadialMaterial(resources)
        {
            Additive = true,
            SizeMultiplier = Vector2.One,
            OuterAlpha = 1
        };

        glowMaterial = new SunRadialMaterial(resources)
        {
            SizeMultiplier = Vector2.One
        };
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

    private TextureShape? ResolveShape(string? name)
    {
        if (!string.IsNullOrEmpty(name) &&
            resources.TryGetShape(name, out var shape) &&
            shape.HasValue)
        {
            return shape.Value;
        }
        return null;
    }

    public unsafe void Render(Sun sun, Color4 background, RenderContext render, Rectangle? viewport) =>
        Render(sun, background, render, viewport, 1f);

    public unsafe void Render(Sun sun, Color4 background, RenderContext render, Rectangle? viewport, float zoom)
        => Render(sun, background, render, viewport, zoom, 0);

    public unsafe void Render(Sun sun, Color4 background, RenderContext render, Rectangle? viewport, float zoom, float angleOffset)
        => Render(sun, background, render, viewport, zoom, angleOffset, Vector2.Zero);

    public unsafe void Render(Sun sun, Color4 background, RenderContext render, Rectangle? viewport, float zoom, float angleOffset, Vector2 screenOffset)
        => Render(sun, background, render, viewport, zoom, angleOffset, screenOffset, true);

    public unsafe void Render(Sun sun, Color4 background, RenderContext render, Rectangle? viewport, float zoom, float angleOffset, Vector2 screenOffset, bool fitFullBillboard)
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

        float renderSize = MathF.Max(1, sun.Radius);
        if (fitFullBillboard)
        {
            if (sun.GlowSprite != null)
                renderSize = MathF.Max(renderSize, sun.Radius * MathF.Max(1, sun.GlowScale));
            if (sun.CenterSprite != null)
                renderSize = MathF.Max(renderSize, sun.Radius * MathF.Max(1, sun.CenterScale));
        }
        if (sun.SpinesSprite != null && sun.Spines is { Count: > 0 })
        {
            renderSize = MathF.Max(renderSize, sun.Radius * MathF.Max(1, sun.SpinesScale));
            foreach (var s in sun.Spines)
            {
                var lengthScale = MathF.Max(0.001f, s.LengthScale);
                var multMax = MathF.Max(s.WidthScale / lengthScale, lengthScale);
                renderSize = MathF.Max(renderSize, sun.Radius * MathF.Max(1, sun.SpinesScale) * multMax);
            }
        }

        //camera
        zoom = MathHelper.Clamp(zoom, 0.1f, 10f);
        cam.Update(render.CurrentViewport.Width, render.CurrentViewport.Height, new Vector3(0, 0, -(renderSize * 0.9f) / zoom),
            Vector3.Zero);
        render.SetCamera(cam);
        render.Cull = false;
        render.DepthEnabled = false;

        var sunPosition = new Vector3(
            screenOffset.X * renderSize / zoom,
            -screenOffset.Y * renderSize / zoom,
            0);
        var count = SunRenderer.GetVertexCount(sun);
        EnsureCapacity(render, count);
        var centerShape = ResolveShape(sun.CenterSprite);
        var glowShape = ResolveShape(sun.GlowSprite);
        var spinesShape = ResolveShape(sun.SpinesSprite);
        SunRenderer.CreateVertices(vertices, sunPosition, sun, angleOffset,
            centerShape?.Dimensions,
            glowShape?.Dimensions,
            spinesShape?.Dimensions);
        vertexBuffer.SetData<VertexBillboardColor2>(vertices.AsSpan().Slice(0, count));
        spineMaterial.Texture = spinesShape?.Texture ?? sun.SpinesSprite;
        centerMaterial.Texture = centerShape?.Texture ?? sun.CenterSprite;
        glowMaterial.Texture = glowShape?.Texture ?? sun.GlowSprite;
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
        if (sun.Spines is { Count: >0 })
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
