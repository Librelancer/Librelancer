// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Graphics.Backends;

namespace LibreLancer.Graphics;

/// <summary>
/// G-Buffer for deferred rendering with multiple render targets.
/// Contains: Position (RGBA16F), Normal (RGBA16F), Albedo (RGBA8), Material (RGBA8), Depth (32F).
/// Requires OpenGL 3.0+ with MRT support.
/// </summary>
/// <remarks>
/// G-Buffer Layout:
/// - RT0: Position (RGBA16F) - World-space position
/// - RT1: Normal (RGBA16F) - World-space normal (encoded)
/// - RT2: Albedo (RGBA8) - Diffuse color + alpha mask
/// - RT3: Material (RGBA8) - R=metallic, G=roughness, B=AO, A=emissive
/// - Depth: 32F depth buffer for depth reconstruction
///
/// Use BindForWriting() during geometry pass, BindForReading() during lighting pass.
/// Textures are bound to consecutive texture units starting from the specified unit.
///
/// Extends RenderTarget to integrate with RenderContext's render target tracking system.
/// This allows the G-Buffer to be set as the active render target, preventing ApplyRenderTarget
/// from overwriting the G-Buffer framebuffer binding during geometry pass draws.
/// </remarks>
public class GBuffer : RenderTarget
{
    internal IGBuffer Backing;
    private bool disposed;

    /// <summary>
    /// Width of the G-Buffer in pixels.
    /// </summary>
    public int Width => Backing?.Width ?? 0;

    /// <summary>
    /// Height of the G-Buffer in pixels.
    /// </summary>
    public int Height => Backing?.Height ?? 0;

    /// <summary>
    /// Number of G-Buffer texture attachments (Position, Normal, Albedo, Material, Depth).
    /// </summary>
    public const int TextureCount = 5;

    public GBuffer(RenderContext context, int width, int height)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be positive");
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be positive");

        Backing = context.Backend.CreateGBuffer(width, height);
        // Set the Target property from RenderTarget base class so that
        // RenderContext's ApplyRenderTarget can properly track and bind us
        Target = Backing;
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
            throw new ObjectDisposedException(nameof(GBuffer));
    }

    /// <summary>
    /// Bind the G-Buffer framebuffer for writing during geometry pass.
    /// </summary>
    public void BindForWriting()
    {
        ThrowIfDisposed();
        Backing.BindForWriting();
    }

    /// <summary>
    /// Bind the G-Buffer textures for reading during lighting pass.
    /// Textures are bound to consecutive texture units: Position, Normal, Albedo, Material, Depth.
    /// </summary>
    /// <param name="startUnit">First texture unit to bind to (default 0).</param>
    public void BindForReading(int startUnit = 0)
    {
        ThrowIfDisposed();
        Backing.BindForReading(startUnit);
    }

    /// <summary>
    /// Clear the G-Buffer color and depth attachments.
    /// </summary>
    public void Clear()
    {
        ThrowIfDisposed();
        Backing.Clear();
    }

    /// <summary>
    /// Resize the G-Buffer. All textures are recreated at the new size.
    /// </summary>
    /// <param name="width">New width in pixels.</param>
    /// <param name="height">New height in pixels.</param>
    public void Resize(int width, int height)
    {
        ThrowIfDisposed();
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be positive");
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be positive");
        Backing.Resize(width, height);
    }

    /// <summary>
    /// Unbind the G-Buffer (return to default framebuffer).
    /// </summary>
    public void Unbind()
    {
        ThrowIfDisposed();
        Backing.Unbind();
    }

    /// <summary>
    /// Bind only the depth texture for reading (used by depth copy pass).
    /// </summary>
    /// <param name="unit">Texture unit to bind to (default 0).</param>
    public void BindDepthForReading(int unit = 0)
    {
        ThrowIfDisposed();
        Backing.BindDepthForReading(unit);
    }

    /// <summary>
    /// Releases all resources used by the G-Buffer.
    /// </summary>
    public override void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        Backing?.Dispose();
        Backing = null;
        Target = null;
    }
}
