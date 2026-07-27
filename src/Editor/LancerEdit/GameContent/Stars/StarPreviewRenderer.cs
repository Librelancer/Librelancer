using System;
using LibreLancer;
using LibreLancer.Data.GameData.Archetypes;
using LibreLancer.Graphics;
using LibreLancer.Render;

namespace LancerEdit.GameContent.Stars;

public sealed class StarPreviewRenderer : IDisposable
{
    public const float ThumbnailZoom = 1f;
    public const int ThumbnailTextureSize = 128;
    public const string ThumbnailSource = "Star Preview thumbnail";
    public static readonly Color4 ThumbnailBackground = new(0.008f, 0.011f, 0.022f, 1f);
    public const float PreviewZoom = 1f;
    public static readonly Color4 PreviewBackground = new(0.008f, 0.011f, 0.022f, 1f);

    private readonly Popups.SunImmediateRenderer renderer;

    public StarPreviewRenderer(GameDataContext context)
    {
        StarResourceLoader.EnsureLoaded(context);
        renderer = new Popups.SunImmediateRenderer(context.Resources);
    }

    public void Render(Sun sun, Color4 background, RenderContext renderContext, Rectangle viewport) =>
        renderer.Render(sun, background, renderContext, viewport, ThumbnailZoom);

    public void Render(Sun sun, Color4 background, RenderContext renderContext, Rectangle viewport, float zoom) =>
        renderer.Render(sun, background, renderContext, viewport, zoom);

    public void Render(Sun sun, Color4 background, RenderContext renderContext, Rectangle viewport, float zoom, float angleOffset) =>
        renderer.Render(sun, background, renderContext, viewport, zoom, angleOffset);

    public void Render(Sun sun, Color4 background, RenderContext renderContext, Rectangle viewport, float zoom, float angleOffset, System.Numerics.Vector2 screenOffset) =>
        renderer.Render(sun, background, renderContext, viewport, zoom, angleOffset, screenOffset);

    public Texture2D RenderTexture(Sun sun, Color4 background, RenderContext renderContext, int width, int height, float zoom = ThumbnailZoom)
    {
        var restore = renderContext.RenderTarget;
        var target = new RenderTarget2D(renderContext, width, height);
        renderContext.RenderTarget = target;
        renderer.Render(sun, background, renderContext, new Rectangle(0, 0, width, height), zoom);
        renderContext.RenderTarget = restore;
        target.Dispose(true);
        return target.Texture;
    }

    public static float GetVisualRadius(Sun sun)
    {
        var radius = MathF.Max(1, sun.Radius);
        var visualRadius = radius;
        if (!string.IsNullOrWhiteSpace(sun.GlowSprite))
            visualRadius = MathF.Max(visualRadius, radius * MathF.Max(1, sun.GlowScale));
        if (!string.IsNullOrWhiteSpace(sun.CenterSprite))
            visualRadius = MathF.Max(visualRadius, radius * MathF.Max(1, sun.CenterScale));
        if (!string.IsNullOrWhiteSpace(sun.SpinesSprite) && sun.Spines is { Count: > 0 })
        {
            foreach (var spine in sun.Spines)
            {
                var mult = MathF.Max(spine.WidthScale / MathF.Max(0.001f, spine.LengthScale), spine.LengthScale);
                visualRadius = MathF.Max(visualRadius, radius * MathF.Max(1, sun.SpinesScale) * mult);
            }
        }
        return visualRadius;
    }

    public void Dispose() => renderer.Dispose();
}
