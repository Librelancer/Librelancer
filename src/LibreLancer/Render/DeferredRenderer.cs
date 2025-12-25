// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Graphics;
using LibreLancer.Shaders;

namespace LibreLancer.Render;

/// <summary>
/// Deferred rendering pipeline that uses G-Buffer for efficient multi-light rendering.
/// Supports PBR materials with metallic/roughness workflow.
/// </summary>
public class DeferredRenderer : IDisposable
{
    private readonly RenderContext rstate;
    private GBuffer gbuffer;
    private GBufferDebugView debugView;
    private RenderTarget previousRenderTarget;
    private Shader lightingShader;
    private Shader depthCopyShader;
    private bool depthCopyShaderInitAttempted;
    private RenderStateSnapshot lightingState;
    private bool lightingStateCaptured;

    private int width;
    private int height;
    private bool disposed;

    /// <summary>
    /// Whether deferred rendering is enabled and available.
    /// Requires OpenGL 4.6 in the current configuration.
    /// </summary>
    public bool IsEnabled { get; set; } = false;  // Disabled by default - enable with Ctrl+Shift+G

    /// <summary>
    /// Whether deferred rendering is supported on this system.
    /// </summary>
    public bool IsSupported { get; private set; }

    /// <summary>
    /// The G-Buffer containing geometry data for deferred lighting.
    /// </summary>
    public GBuffer GBuffer => gbuffer;

    /// <summary>
    /// Debug visualization for G-Buffer inspection.
    /// Set DebugView.Mode to enable visualization of individual G-Buffer channels.
    /// </summary>
    public GBufferDebugView DebugView => debugView;

    public DeferredRenderer(RenderContext rstate)
    {
        this.rstate = rstate;

        // Check for required features (OpenGL 3.0+ with MRT)
        IsSupported = CheckSupport();

        if (IsSupported)
        {
            FLLog.Info("DeferredRenderer", "Deferred rendering supported (disabled by default)");
            debugView = new GBufferDebugView(rstate);
        }
        else
        {
            FLLog.Warning("DeferredRenderer", "Deferred rendering not supported, falling back to forward rendering");
            IsEnabled = false;
        }
    }

    private bool CheckSupport()
    {
        // Deferred rendering requires OpenGL 4.6 features for best performance
        // MRT (Multiple Render Targets) is baseline OpenGL 3.0, but we want 4.6 for:
        // - SPIR-V shaders
        // - Direct state access
        // - Better performance characteristics
        return rstate.HasFeature(GraphicsFeature.OpenGL46);
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
            throw new ObjectDisposedException(nameof(DeferredRenderer));
    }

    /// <summary>
    /// Initialize or resize the deferred rendering resources.
    /// </summary>
    public void Resize(int newWidth, int newHeight)
    {
        ThrowIfDisposed();

        if (newWidth == width && newHeight == height && gbuffer != null)
            return;

        if (newWidth <= 0 || newHeight <= 0)
            return;

        width = newWidth;
        height = newHeight;

        // Dispose old G-Buffer
        gbuffer?.Dispose();

        // Create new G-Buffer
        if (IsSupported && IsEnabled)
        {
            gbuffer = new GBuffer(rstate, width, height);
            FLLog.Info("DeferredRenderer", $"G-Buffer resized to {width}x{height}");
        }
    }

    /// <summary>
    /// Begin the geometry pass - renders opaque objects to the G-Buffer.
    /// </summary>
    public void BeginGeometryPass()
    {
        ThrowIfDisposed();

        if (gbuffer == null)
        {
            FLLog.Warning("DeferredRenderer", "BeginGeometryPass called but gbuffer is null!");
            return;
        }

        // Save the current render target so we can restore it in EndGeometryPass
        previousRenderTarget = rstate.RenderTarget;

        // Set the G-Buffer as the active render target in RenderContext
        // This is CRITICAL: it tells ApplyRenderTarget to bind the G-Buffer framebuffer
        // instead of overwriting with MSAA or other targets during Draw() calls
        rstate.RenderTarget = gbuffer;

        // Push viewport for G-Buffer to ensure correct rendering
        rstate.PushViewport(new Rectangle(0, 0, width, height));

        // Clear the G-Buffer
        gbuffer.Clear();

        // Set up depth testing for geometry pass
        rstate.DepthEnabled = true;
        rstate.DepthWrite = true;
        rstate.ColorWrite = true;

        // Enable deferred mode for materials - they will use G-Buffer fill shaders
        RenderMaterial.DeferredMode = true;
    }

    /// <summary>
    /// End the geometry pass.
    /// </summary>
    public void EndGeometryPass()
    {
        ThrowIfDisposed();

        // Disable deferred mode - materials will use forward shaders
        RenderMaterial.DeferredMode = false;

        if (gbuffer == null)
            return;

        // Pop the viewport that was pushed in BeginGeometryPass
        rstate.PopViewport();

        // Restore the previous render target (e.g., MSAA)
        rstate.RenderTarget = previousRenderTarget;
        previousRenderTarget = null;
    }

    /// <summary>
    /// Perform the deferred lighting pass.
    /// Reads from G-Buffer and applies lighting to produce the final image.
    /// </summary>
    /// <param name="target">Output render target (typically MSAA buffer)</param>
    /// <param name="camera">Camera for view/projection matrices</param>
    /// <param name="lighting">Scene lighting configuration</param>
    public void PerformLightingPass(RenderTarget target, ICamera camera, ref Lighting lighting)
    {
        ThrowIfDisposed();

        if (gbuffer == null)
        {
            FLLog.Warning("DeferredRenderer", "Lighting pass skipped: no G-Buffer");
            return;
        }

        // Get or cache lighting shader
        if (lightingShader == null)
            lightingShader = AllShaders.DeferredLighting?.Get(0);
        if (lightingShader == null)
        {
            FLLog.Warning("DeferredRenderer", "DeferredLighting shader unavailable, lighting pass skipped");
            return;
        }

        lightingState = new RenderStateSnapshot(rstate);
        lightingStateCaptured = true;

        // Bind the target framebuffer
        rstate.RenderTarget = target;

        // Disable depth testing for fullscreen quad
        rstate.DepthEnabled = false;
        rstate.DepthWrite = false;
        rstate.ColorWrite = true;

        // Bind G-Buffer textures for reading (texture units 0-4)
        gbuffer.BindForReading();

        // Set lighting uniforms (cbuffer b2)
        RenderMaterial.SetLights(lightingShader, ref lighting, 0);

        // Configure render state for fullscreen pass
        rstate.Cull = false;
        rstate.BlendMode = BlendMode.Opaque;
        rstate.Shader = lightingShader;

        // Execute fullscreen lighting
        rstate.DrawFullscreenTriangle();
    }

    /// <summary>
    /// Restore render state after lighting pass.
    /// </summary>
    public void EndLightingPass()
    {
        ThrowIfDisposed();
        if (!lightingStateCaptured)
            return;

        lightingState.Restore(rstate);
        lightingStateCaptured = false;
        rstate.Apply();
    }

    /// <summary>
    /// Check if the current frame should use deferred rendering.
    /// </summary>
    public bool ShouldUseDeferred()
    {
        return IsSupported && IsEnabled && gbuffer != null;
    }

    /// <summary>
    /// Render the G-Buffer debug visualization if enabled.
    /// Call this after the lighting pass to overlay debug output.
    /// </summary>
    public void RenderDebugView(RenderTarget target = null)
    {
        ThrowIfDisposed();

        if (debugView != null && debugView.Mode != GBufferDebugMode.None && gbuffer != null)
        {
            debugView.Render(gbuffer, target);
        }
    }

    /// <summary>
    /// Copies G-Buffer depth to the target's depth buffer for transparent object occlusion.
    /// Must be called after PerformLightingPass and before transparent rendering.
    /// </summary>
    /// <param name="target">Target render buffer (typically MSAA buffer)</param>
    public void BlitDepthToTarget(RenderTarget target)
    {
        ThrowIfDisposed();

        if (gbuffer == null)
            return;

        // Lazy shader initialization with graceful fallback (only attempt once)
        if (depthCopyShader == null)
        {
            if (depthCopyShaderInitAttempted)
                return;  // Already tried and failed, skip silently

            depthCopyShaderInitAttempted = true;
            depthCopyShader = AllShaders.DepthCopy?.Get(0);
            if (depthCopyShader == null)
            {
                FLLog.Warning("DeferredRenderer", "DepthCopy shader unavailable, transparent occlusion may be incorrect");
                return;
            }
        }

        var previousState = new RenderStateSnapshot(rstate);

        // Set render target to write depth
        rstate.RenderTarget = target;

        // Configure depth state for unconditional depth writes
        rstate.DepthEnabled = true;
        rstate.DepthWrite = true;
        rstate.DepthFunction = DepthFunction.Always;  // CRITICAL: Must always write
        rstate.ColorWrite = false;  // Only depth matters
        rstate.Cull = false;
        rstate.BlendMode = BlendMode.Opaque;

        // Bind G-Buffer depth texture
        gbuffer.BindDepthForReading(0);

        // Draw fullscreen triangle to copy depth
        rstate.Shader = depthCopyShader;
        rstate.DrawFullscreenTriangle();

        previousState.Restore(rstate);
        rstate.Apply();
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;

        debugView?.Dispose();
        debugView = null;

        gbuffer?.Dispose();
        gbuffer = null;
    }
}
