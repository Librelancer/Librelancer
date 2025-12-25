// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Runtime.InteropServices;
using LibreLancer.Graphics;
using LibreLancer.Shaders;

namespace LibreLancer.Render;

/// <summary>
/// Debug visualization modes for G-Buffer inspection.
/// </summary>
public enum GBufferDebugMode
{
    /// <summary>Disabled - no debug output</summary>
    None = -1,
    /// <summary>World position (XYZ as RGB, fractional)</summary>
    Position = 0,
    /// <summary>World normal (remapped from [-1,1] to [0,1])</summary>
    Normal = 1,
    /// <summary>Albedo/diffuse color</summary>
    Albedo = 2,
    /// <summary>Metallic factor (grayscale)</summary>
    Metallic = 3,
    /// <summary>Roughness factor (grayscale)</summary>
    Roughness = 4,
    /// <summary>Ambient occlusion (grayscale)</summary>
    AmbientOcclusion = 5,
    /// <summary>Emissive intensity (grayscale)</summary>
    Emissive = 6,
    /// <summary>Linearized depth (grayscale)</summary>
    Depth = 7,
    /// <summary>Combined material (R=Metallic, G=Roughness, B=AO)</summary>
    MaterialCombined = 8
}

/// <summary>
/// Provides debug visualization for the G-Buffer in deferred rendering.
/// Allows inspecting individual G-Buffer channels to verify correct rendering.
/// </summary>
public class GBufferDebugView : IDisposable
{
    private readonly RenderContext rstate;
    private Shader debugShader;
    private bool disposed;

    /// <summary>
    /// Current debug mode. Set to None to disable debug visualization.
    /// </summary>
    public GBufferDebugMode Mode { get; set; } = GBufferDebugMode.None;

    /// <summary>
    /// Near plane distance for depth linearization.
    /// </summary>
    public float DepthNear { get; set; } = 1.0f;

    /// <summary>
    /// Far plane distance for depth linearization.
    /// </summary>
    public float DepthFar { get; set; } = 100000.0f;

    // Matches cbuffer DebugParameters in GBuffer_Debug.frag.hlsl
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct DebugParameters
    {
        public Vector4 DebugParams; // x = mode, y = near, z = far, w = unused
    }

    public GBufferDebugView(RenderContext rstate)
    {
        this.rstate = rstate;
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
            throw new ObjectDisposedException(nameof(GBufferDebugView));
    }

    /// <summary>
    /// Renders the G-Buffer debug visualization to the current render target.
    /// </summary>
    /// <param name="gbuffer">The G-Buffer to visualize.</param>
    /// <param name="target">Optional render target. Uses default if null.</param>
    public void Render(GBuffer gbuffer, RenderTarget target = null)
    {
        ThrowIfDisposed();

        if (Mode == GBufferDebugMode.None)
            return;

        if (gbuffer == null)
            return;

        // Get or create the shader instance
        if (debugShader == null)
        {
            debugShader = AllShaders.GBuffer_Debug.Get(0);
        }

        var previousState = new RenderStateSnapshot(rstate);

        // Set render target
        rstate.RenderTarget = target;

        // Disable depth testing for fullscreen pass
        rstate.DepthEnabled = false;
        rstate.DepthWrite = false;

        // Bind G-Buffer textures
        gbuffer.BindForReading();

        // Set shader uniforms (packed as float4: x=mode, y=near, z=far, w=unused)
        var debugParams = new DebugParameters
        {
            DebugParams = new Vector4((float)Mode, DepthNear, DepthFar, 0f)
        };
        debugShader.SetUniformBlock(3, ref debugParams);

        // Apply shader and draw fullscreen triangle
        rstate.Cull = false;
        rstate.BlendMode = BlendMode.Opaque;
        rstate.Shader = debugShader;
        rstate.DrawFullscreenTriangle();

        previousState.Restore(rstate);
        rstate.Apply();
    }

    /// <summary>
    /// Cycles to the next debug mode.
    /// </summary>
    public void CycleMode()
    {
        if (Mode == GBufferDebugMode.None)
        {
            Mode = GBufferDebugMode.Position;
        }
        else if (Mode == GBufferDebugMode.MaterialCombined)
        {
            Mode = GBufferDebugMode.None;
        }
        else
        {
            Mode = (GBufferDebugMode)((int)Mode + 1);
        }
    }

    /// <summary>
    /// Gets a display name for the current debug mode.
    /// </summary>
    public string GetModeDisplayName()
    {
        return Mode switch
        {
            GBufferDebugMode.None => "Off",
            GBufferDebugMode.Position => "Position",
            GBufferDebugMode.Normal => "Normal",
            GBufferDebugMode.Albedo => "Albedo",
            GBufferDebugMode.Metallic => "Metallic",
            GBufferDebugMode.Roughness => "Roughness",
            GBufferDebugMode.AmbientOcclusion => "AO",
            GBufferDebugMode.Emissive => "Emissive",
            GBufferDebugMode.Depth => "Depth",
            GBufferDebugMode.MaterialCombined => "Material",
            _ => "Unknown"
        };
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        // Shaders are managed by ShaderBundle, no need to dispose
    }
}
