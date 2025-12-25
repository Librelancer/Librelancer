// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.Graphics.Backends;

/// <summary>
/// G-Buffer (Geometry Buffer) for deferred rendering.
/// Contains multiple render targets for storing geometry pass data:
/// - Position (world space)
/// - Normal (world space)
/// - Albedo (diffuse color)
/// - Material properties (metallic, roughness, etc.)
/// </summary>
/// <remarks>
/// Individual texture properties are exposed for debug visualization and advanced use cases.
/// For standard deferred lighting, use BindForReading() which binds all textures at once.
/// Extends IRenderTarget to integrate with RenderContext's render target tracking system.
/// </remarks>
public interface IGBuffer : IRenderTarget
{
    /// <summary>
    /// Width of all G-Buffer textures.
    /// </summary>
    int Width { get; }

    /// <summary>
    /// Height of all G-Buffer textures.
    /// </summary>
    int Height { get; }

    /// <summary>
    /// Position texture (RGBA16F - world space XYZ + unused alpha).
    /// </summary>
    ITexture2D PositionTexture { get; }

    /// <summary>
    /// Normal texture (RGBA16F - world space normal XYZ + unused alpha).
    /// </summary>
    ITexture2D NormalTexture { get; }

    /// <summary>
    /// Albedo texture (RGBA8 - diffuse RGB + alpha for transparency mask).
    /// </summary>
    ITexture2D AlbedoTexture { get; }

    /// <summary>
    /// Material texture (RGBA8 - R: metallic, G: roughness, B: AO, A: emissive).
    /// </summary>
    ITexture2D MaterialTexture { get; }

    /// <summary>
    /// Depth texture for depth reconstruction and testing.
    /// </summary>
    ITexture2D DepthTexture { get; }

    /// <summary>
    /// Bind the G-Buffer for writing (geometry pass).
    /// All color attachments and depth are bound as render targets.
    /// </summary>
    void BindForWriting();

    /// <summary>
    /// Bind the G-Buffer textures for reading (lighting pass).
    /// Textures are bound to the specified starting texture unit.
    /// </summary>
    /// <param name="startUnit">First texture unit to use (default 0).</param>
    void BindForReading(int startUnit = 0);

    /// <summary>
    /// Clear all G-Buffer attachments.
    /// </summary>
    void Clear();

    /// <summary>
    /// Resize the G-Buffer to new dimensions.
    /// </summary>
    void Resize(int width, int height);

    /// <summary>
    /// Unbind the G-Buffer (return to default framebuffer).
    /// </summary>
    void Unbind();

    /// <summary>
    /// Bind only the depth texture for reading (used by depth copy pass).
    /// </summary>
    /// <param name="unit">Texture unit to bind to.</param>
    void BindDepthForReading(int unit);
}
