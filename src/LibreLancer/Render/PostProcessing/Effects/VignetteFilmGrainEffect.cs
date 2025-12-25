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
/// Combined vignette and film grain post-processing effect.
/// Vignette darkens screen corners for a cinematic look.
/// Film grain adds subtle animated noise for film-like quality.
/// </summary>
public class VignetteFilmGrainEffect : PostEffectBase
{
    private float accumulatedTime;

    // Matches cbuffer PostFXParams in PostFX_VignetteGrain.frag.hlsl
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct VignetteGrainParams
    {
        public float VignetteIntensity;  // 0.0 (off) to 2.0 (strong)
        public float VignetteSoftness;   // 0.1 (sharp) to 1.0 (soft)
        public float GrainIntensity;     // 0.0 (off) to 0.2 (strong)
        public float Time;               // Accumulated time for animation
        public Vector2 Resolution;       // Screen resolution
        public Vector2 Padding;          // Alignment padding
    }

    /// <inheritdoc/>
    public override string Name => "Vignette + Film Grain";

    /// <inheritdoc/>
    public override bool IsActive =>
        IsEnabled && Settings != null && (Settings.VignetteEnabled || Settings.FilmGrainEnabled);

    /// <inheritdoc/>
    public override int Priority => 100;  // Mid-priority effect

    /// <inheritdoc/>
    public override PostEffectInputs RequiredInputs => PostEffectInputs.Color;

    /// <summary>
    /// Settings reference for parameter access.
    /// </summary>
    public PostProcessingSettings Settings { get; set; }

    /// <inheritdoc/>
    public override void Initialize(RenderContext context)
    {
        base.Initialize(context);

        // Get shader from pre-compiled bundle
        EffectShader = AllShaders.PostFX_VignetteGrain?.Get(0);
        if (EffectShader == null)
        {
            FLLog.Warning("PostFX", "VignetteGrain shader not available, effect disabled");
            IsEnabled = false;
        }
    }

    /// <inheritdoc/>
    public override void Render(ref PostEffectContext context)
    {
        if (EffectShader == null || Settings == null)
            return;

        // Accumulate time for grain animation
        accumulatedTime += context.DeltaTime;
        if (accumulatedTime > 10000f)
            accumulatedTime -= 10000f;

        // Set up fullscreen pass
        var snapshot = SetupFullscreenPass(context.Output, EffectShader);

        try
        {
            float vignetteIntensity = Settings.VignetteEnabled
                ? Math.Clamp(Settings.VignetteIntensity, 0f, 2f)
                : 0f;
            float vignetteSoftness = Settings.VignetteEnabled
                ? Math.Clamp(Settings.VignetteSoftness, 0.1f, 1f)
                : 1f;
            float grainIntensity = Settings.FilmGrainEnabled
                ? Math.Clamp(Settings.FilmGrainIntensity, 0f, 0.2f)
                : 0f;

            // Prepare shader parameters
            // Use effective values (0 if disabled)
            var parameters = new VignetteGrainParams
            {
                VignetteIntensity = vignetteIntensity,
                VignetteSoftness = vignetteSoftness,
                GrainIntensity = grainIntensity,
                Time = accumulatedTime,
                Resolution = new Vector2(context.Width, context.Height),
                Padding = Vector2.Zero
            };

            // Set uniform block (cbuffer b3 for fragment shader)
            EffectShader.SetUniformBlock(3, ref parameters);

            // Bind input texture
            context.Input.BindTo(0);

            // Draw fullscreen triangle
            RenderContext.DrawFullscreenTriangle();
        }
        finally
        {
            // Restore state
            RestoreState(snapshot);
        }
    }

    /// <inheritdoc/>
    public override void Dispose()
    {
        // Shader is managed by AllShaders, don't dispose here
        base.Dispose();
    }
}
