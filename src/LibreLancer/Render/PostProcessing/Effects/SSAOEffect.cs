// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Runtime.InteropServices;
using LibreLancer.Graphics;
using LibreLancer.Shaders;

namespace LibreLancer.Render.PostProcessing.Effects;

/// <summary>
/// Screen-Space Ambient Occlusion effect using GTAO (Ground Truth Ambient Occlusion) algorithm.
/// Provides high-quality ambient occlusion with better edge handling and stability than traditional SSAO.
/// Requires deferred rendering mode (G-Buffer access).
/// </summary>
public class SSAOEffect : PostEffectBase
{
    private Shader blurShader;
    private RenderTarget2D aoBuffer;  // Intermediate AO-only output (single-channel in RGB)
    private bool permanentlyDisabled;

    // GPU-aligned struct for GTAO parameters (cbuffer b3)
    // Note: Camera matrices come from Camera.hlsl (cbuffer b1), not here
    // Note: SPIRV-Cross requires all types in cbuffer to be the same (floats)
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    struct SSAOParams
    {
        public float Radius;              // 0-4
        public float Falloff;             // 4-8
        public float Intensity;           // 8-12
        public float Directions;          // 12-16 (cast to int in shader)
        public float Steps;               // 16-20 (cast to int in shader)
        public float Padding1;            // 20-24
        public float Padding2;            // 24-28
        public float Padding3;            // 28-32
        public Vector4 ResolutionPacked;  // 32-48: xy=resolution, zw=1/resolution
    }

    // Blur pass parameters (simpler struct)
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    struct BlurParams
    {
        public Vector2 Resolution;
        public Vector2 InvResolution;
        public float DepthThreshold;      // Edge detection threshold
        public float Padding1;
        public float Padding2;
        public float Padding3;
    }

    /// <inheritdoc/>
    public override string Name => "SSAO";

    /// <inheritdoc/>
    public override bool IsActive => IsEnabled && !permanentlyDisabled && Settings != null && Settings.SSAOEnabled;

    /// <inheritdoc/>
    public override int Priority => 50;  // Early in chain, before color grading

    /// <inheritdoc/>
    public override PostEffectInputs RequiredInputs =>
        PostEffectInputs.Color | PostEffectInputs.Depth | PostEffectInputs.Normals;

    /// <summary>
    /// Settings reference for parameter access.
    /// </summary>
    public PostProcessingSettings Settings { get; set; }

    /// <summary>
    /// Reference to the PostProcessingManager for G-Buffer access.
    /// </summary>
    public PostProcessingManager Manager { get; set; }

    /// <inheritdoc/>
    public override void Initialize(RenderContext context)
    {
        base.Initialize(context);

        if (permanentlyDisabled)
            return;

        try
        {
            // Get shaders from pre-compiled bundles
            EffectShader = AllShaders.PostFX_GTAO?.Get(0);
            blurShader = AllShaders.PostFX_GTAOBlur?.Get(0);

            if (EffectShader == null || blurShader == null)
            {
                FLLog.Warning("SSAO", "SSAO shaders not available, effect disabled");
                IsEnabled = false;
                permanentlyDisabled = true;
                return;
            }
        }
        catch (Exception ex)
        {
            FLLog.Error("SSAO", $"Shader compilation failed: {ex.Message}");
            FLLog.Warning("SSAO", "SSAO disabled - GPU may not support required features");
            IsEnabled = false;
            permanentlyDisabled = true;
        }
    }

    /// <inheritdoc/>
    public override void OnResize(int width, int height)
    {
        base.OnResize(width, height);

        if (permanentlyDisabled)
            return;

        // Dispose old AO buffer
        aoBuffer?.Dispose();
        aoBuffer = null;

        // Create new AO buffer (single-channel AO stored in RGB)
        try
        {
            // Use RGBA8 since R8 may not be universally supported
            aoBuffer = new RenderTarget2D(RenderContext, width, height);
        }
        catch (Exception ex)
        {
            FLLog.Warning("SSAO", $"AO buffer allocation failed: {ex.Message}");
            IsEnabled = false;
            // Don't set permanentlyDisabled - may work on next resize
        }
    }

    /// <inheritdoc/>
    public override void Render(ref PostEffectContext context)
    {
        // Check for permanent disable
        if (permanentlyDisabled || EffectShader == null)
            return;

        // Check if effect is enabled in settings
        if (Settings == null || !Settings.SSAOEnabled)
            return;

        // Check if G-Buffer is available
        if (Manager?.GBuffer == null || !Manager.HasGBuffer)
            return;

        // Check if AO buffer is available
        if (aoBuffer == null)
            return;

        // Pass 1: GTAO calculation -> aoBuffer
        RenderAOPass(ref context);

        // Pass 2: Blur + composite -> context.Output (blur enabled) or just composite (blur disabled)
        RenderBlurOrCompositePass(ref context, Settings.SSAOBlurEnabled);
    }

    private void RenderAOPass(ref PostEffectContext context)
    {
        var snapshot = SetupFullscreenPass(aoBuffer, EffectShader);

        try
        {
            // Prepare shader parameters (camera matrices come from Camera.hlsl cbuffer b1)
            float radius = Math.Clamp(Settings.SSAORadius, 0.5f, 10f);
            float falloff = Math.Clamp(Settings.SSAOFalloff, 0.1f, 3f);
            float intensity = Math.Clamp(Settings.SSAOIntensity, 0f, 3f);
            float directions = Math.Clamp(Settings.SSAODirections, 2, 8);
            float steps = Math.Clamp(Settings.SSAOSteps, 2, 8);

            var parameters = new SSAOParams
            {
                Radius = radius,
                Falloff = falloff,
                Intensity = intensity,
                Directions = directions,
                Steps = steps,
                Padding1 = 0,
                Padding2 = 0,
                Padding3 = 0,
                ResolutionPacked = new Vector4(
                    context.Width,
                    context.Height,
                    1.0f / context.Width,
                    1.0f / context.Height
                )
            };

            // Set uniform block for SSAO params (b3)
            EffectShader.SetUniformBlock(3, ref parameters);

            // Camera matrices are set via Camera.hlsl (cbuffer b1) - bound by RenderContext

            // Bind G-Buffer textures for reading
            // t0: Position, t1: Normal, t2: Albedo, t3: Material, t4: Depth
            Manager.GBuffer.BindForReading(0);

            // Draw fullscreen triangle
            RenderContext.DrawFullscreenTriangle();
        }
        finally
        {
            RestoreState(snapshot);
        }
    }

    /// <summary>
    /// Renders the blur/composite pass. Combines AO with scene color using bilateral blur.
    /// </summary>
    /// <param name="context">Post effect context</param>
    /// <param name="enableBlur">If true, applies bilateral blur; if false, just composites</param>
    private void RenderBlurOrCompositePass(ref PostEffectContext context, bool enableBlur)
    {
        var snapshot = SetupFullscreenPass(context.Output, blurShader);

        try
        {
            float depthThreshold = enableBlur
                ? Math.Clamp(Settings.SSAOBlurDepthThreshold, 0.001f, 10f)
                : 0f;

            var parameters = new BlurParams
            {
                Resolution = new Vector2(context.Width, context.Height),
                InvResolution = new Vector2(1.0f / context.Width, 1.0f / context.Height),
                DepthThreshold = depthThreshold,  // 0 = no blur, just passthrough
                Padding1 = 0,
                Padding2 = 0,
                Padding3 = 0
            };

            blurShader.SetUniformBlock(3, ref parameters);

            // Bind AO buffer (t0)
            aoBuffer.Texture.BindTo(0);

            // Bind depth for edge detection (t1)
            Manager.GBuffer.BindDepthForReading(1);

            // Bind scene color (t2) for final composite
            context.Input.BindTo(2);

            // Draw fullscreen triangle
            RenderContext.DrawFullscreenTriangle();
        }
        finally
        {
            RestoreState(snapshot);
        }
    }

    /// <inheritdoc/>
    public override void Dispose()
    {
        aoBuffer?.Dispose();
        aoBuffer = null;

        // Shaders are managed by AllShaders, don't dispose here
        base.Dispose();
    }
}
