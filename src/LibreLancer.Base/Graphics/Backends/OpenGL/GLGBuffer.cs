// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer;

namespace LibreLancer.Graphics.Backends.OpenGL;

/// <summary>
/// OpenGL implementation of G-Buffer for deferred rendering.
/// Uses Multiple Render Targets (MRT) to output geometry data in a single pass.
/// Extends GLRenderTarget to integrate with RenderContext's render target tracking.
/// </summary>
class GLGBuffer : GLRenderTarget, IGBuffer
{
    private uint framebuffer;
    private GLTexture2D positionTexture;
    private GLTexture2D normalTexture;
    private GLTexture2D albedoTexture;
    private GLTexture2D materialTexture;
    private GLTexture2D depthTexture;
    private GLRenderContext context;
    private bool disposed;

    // Draw buffer configuration for geometry pass
    private static readonly int[] DrawBuffers = new[]
    {
        GL.GL_COLOR_ATTACHMENT0, // Position
        GL.GL_COLOR_ATTACHMENT1, // Normal
        GL.GL_COLOR_ATTACHMENT2, // Albedo
        GL.GL_COLOR_ATTACHMENT3  // Material
    };

    public int Width { get; private set; }
    public int Height { get; private set; }

    public ITexture2D PositionTexture => positionTexture;
    public ITexture2D NormalTexture => normalTexture;
    public ITexture2D AlbedoTexture => albedoTexture;
    public ITexture2D MaterialTexture => materialTexture;
    public ITexture2D DepthTexture => depthTexture;

    public GLGBuffer(GLRenderContext context, int width, int height)
    {
        this.context = context;
        Width = width;
        Height = height;

        CreateResources();
    }

    private void CreateResources()
    {
        // Create framebuffer
        framebuffer = GL.GenFramebuffer();
        GL.BindFramebuffer(GL.GL_FRAMEBUFFER, framebuffer);

        // Position buffer - RGBA16F (HalfVector4) for high precision world-space positions
        positionTexture = new GLTexture2D(context, Width, Height, false, SurfaceFormat.HalfVector4);
        positionTexture.SetFiltering(TextureFiltering.Nearest);
        GL.FramebufferTexture2D(GL.GL_FRAMEBUFFER, GL.GL_COLOR_ATTACHMENT0,
            GL.GL_TEXTURE_2D, positionTexture.ID, 0);

        // Normal buffer - RGBA16F (HalfVector4) for high precision world-space normals
        normalTexture = new GLTexture2D(context, Width, Height, false, SurfaceFormat.HalfVector4);
        normalTexture.SetFiltering(TextureFiltering.Nearest);
        GL.FramebufferTexture2D(GL.GL_FRAMEBUFFER, GL.GL_COLOR_ATTACHMENT1,
            GL.GL_TEXTURE_2D, normalTexture.ID, 0);

        // Albedo buffer - RGBA8 for diffuse color + alpha mask
        albedoTexture = new GLTexture2D(context, Width, Height, false, SurfaceFormat.Bgra8);
        albedoTexture.SetFiltering(TextureFiltering.Linear);
        GL.FramebufferTexture2D(GL.GL_FRAMEBUFFER, GL.GL_COLOR_ATTACHMENT2,
            GL.GL_TEXTURE_2D, albedoTexture.ID, 0);

        // Material buffer - RGBA8 for PBR properties (metallic, roughness, AO, emissive)
        materialTexture = new GLTexture2D(context, Width, Height, false, SurfaceFormat.Bgra8);
        materialTexture.SetFiltering(TextureFiltering.Nearest);
        GL.FramebufferTexture2D(GL.GL_FRAMEBUFFER, GL.GL_COLOR_ATTACHMENT3,
            GL.GL_TEXTURE_2D, materialTexture.ID, 0);

        // Depth buffer - 32F depth texture for depth reconstruction
        depthTexture = CreateDepthTexture(Width, Height);
        GL.FramebufferTexture2D(GL.GL_FRAMEBUFFER, GL.GL_DEPTH_ATTACHMENT,
            GL.GL_TEXTURE_2D, depthTexture.ID, 0);

        // Set up draw buffers for MRT
        GL.DrawBuffers(DrawBuffers);

        // Verify framebuffer completeness
        var status = GL.CheckFramebufferStatus(GL.GL_FRAMEBUFFER);
        if (status != GL.GL_FRAMEBUFFER_COMPLETE)
        {
            throw new InvalidOperationException($"G-Buffer framebuffer incomplete: 0x{status:X}");
        }

        // Unbind
        GL.BindFramebuffer(GL.GL_FRAMEBUFFER, 0);

        FLLog.Info("GBuffer", $"Created {Width}x{Height} G-Buffer with 4 color attachments + depth");
    }

    private GLTexture2D CreateDepthTexture(int width, int height)
    {
        var tex = new GLTexture2D(context, width, height, false, SurfaceFormat.Depth32F);
        tex.SetFiltering(TextureFiltering.Nearest);
        return tex;
    }

    /// <summary>
    /// Binds the G-Buffer framebuffer. Called by RenderContext's ApplyRenderTarget.
    /// </summary>
    internal override void BindFramebuffer()
    {
        GL.BindFramebuffer(GL.GL_FRAMEBUFFER, framebuffer);
        GL.DrawBuffers(DrawBuffers);
    }

    public void BindForWriting()
    {
        BindFramebuffer();
        // Note: Viewport is NOT set here - it should be managed by RenderContext
        // via PushViewport to ensure proper state tracking and avoid ApplyViewport conflicts
    }

    public void BindForReading(int startUnit = 0)
    {
        // Bind G-Buffer textures to sequential texture units for lighting pass
        GLBind.BindTexture(startUnit + 0, GL.GL_TEXTURE_2D, positionTexture.ID);
        GLBind.BindTexture(startUnit + 1, GL.GL_TEXTURE_2D, normalTexture.ID);
        GLBind.BindTexture(startUnit + 2, GL.GL_TEXTURE_2D, albedoTexture.ID);
        GLBind.BindTexture(startUnit + 3, GL.GL_TEXTURE_2D, materialTexture.ID);
        GLBind.BindTexture(startUnit + 4, GL.GL_TEXTURE_2D, depthTexture.ID);
    }

    public void Clear()
    {
        GL.BindFramebuffer(GL.GL_FRAMEBUFFER, framebuffer);
        context.SetClearColor(Color4.TransparentBlack);
        GL.Clear(GL.GL_COLOR_BUFFER_BIT | GL.GL_DEPTH_BUFFER_BIT);
    }

    public void Resize(int width, int height)
    {
        if (Width == width && Height == height)
            return;

        Width = width;
        Height = height;

        // Dispose old textures
        DisposeTextures();

        // Delete old framebuffer before creating new one
        if (framebuffer != 0)
        {
            GL.DeleteFramebuffer(framebuffer);
            framebuffer = 0;
        }

        // Create new resources at new size
        CreateResources();
    }

    public void Unbind()
    {
        GL.BindFramebuffer(GL.GL_FRAMEBUFFER, 0);
    }

    public void BindDepthForReading(int unit)
    {
        GLBind.BindTexture(unit, GL.GL_TEXTURE_2D, depthTexture.ID);
    }

    private void DisposeTextures()
    {
        positionTexture?.Dispose();
        normalTexture?.Dispose();
        albedoTexture?.Dispose();
        materialTexture?.Dispose();
        depthTexture?.Dispose();
    }

    public override void Dispose()
    {
        if (disposed) return;
        disposed = true;

        DisposeTextures();

        if (framebuffer != 0)
        {
            GL.DeleteFramebuffer(framebuffer);
            framebuffer = 0;
        }
    }
}
