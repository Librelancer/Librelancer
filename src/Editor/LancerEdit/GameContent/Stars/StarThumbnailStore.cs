using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ImGuiNET;
using LibreLancer;
using LibreLancer.Data.GameData.Archetypes;
using LibreLancer.Graphics;
using LibreLancer.ImUI;
using LibreLancer.Render;

namespace LancerEdit.GameContent.Stars;

public static class StarThumbnailStore
{
    private static readonly ConditionalWeakTable<GameDataContext, Cache> Caches = new();

    public static StarThumbnail Get(GameDataContext context, RenderContext renderContext, Sun sun)
    {
        var cache = Caches.GetValue(context, x => new Cache(x));
        return cache.Get(renderContext, sun);
    }

    public static void Clear(GameDataContext context)
    {
        if (Caches.TryGetValue(context, out var cache))
            cache.Clear();
    }

    private sealed class Cache
    {
        private static readonly string CacheVersion =
            $"{StarPreviewRenderer.ThumbnailSource}:{StarPreviewRenderer.ThumbnailTextureSize}:v7";

        private readonly StarPreviewRenderer renderer;
        private readonly Dictionary<string, StarThumbnail> thumbnails = new(StringComparer.OrdinalIgnoreCase);

        public Cache(GameDataContext context) => renderer = new StarPreviewRenderer(context);

        public StarThumbnail Get(RenderContext renderContext, Sun sun)
        {
            var key = $"{CacheVersion}:{sun.Nickname}";
            if (thumbnails.TryGetValue(key, out var thumbnail))
                return thumbnail;

            var texture = renderer.RenderTexture(
                sun,
                StarPreviewRenderer.ThumbnailBackground,
                renderContext,
                StarPreviewRenderer.ThumbnailTextureSize,
                StarPreviewRenderer.ThumbnailTextureSize,
                StarPreviewRenderer.ThumbnailZoom);
            thumbnail = new StarThumbnail(texture, ImGuiHelper.RegisterTexture(texture));
            thumbnails[key] = thumbnail;
            return thumbnail;
        }

        public void Clear()
        {
            foreach (var thumbnail in thumbnails.Values)
            {
                ImGuiHelper.DeregisterTexture(thumbnail.Texture);
                thumbnail.Texture.Dispose();
            }
            thumbnails.Clear();
        }
    }
}

public sealed record StarThumbnail(Texture2D Texture, ImTextureRef TextureId);
