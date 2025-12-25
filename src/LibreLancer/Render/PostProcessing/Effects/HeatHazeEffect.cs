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
/// Heat haze / distortion post-processing effect.
/// Creates animated distortion patterns simulating heat waves or shield effects.
/// </summary>
public class HeatHazeEffect : PostEffectBase
{
    private float accumulatedTime;

    // Matches cbuffer PostFXParams in PostFX_HeatHaze.frag.hlsl
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct HeatHazeParams
    {
        public float Intensity;    // 0.0 (off) to 0.05 (strong)
        public float Speed;        // 0.1 (slow) to 5.0 (fast)
        public float Scale;        // 1.0 (fine) to 20.0 (coarse)
        public float Time;         // Accumulated time for animation
        public Vector2 Resolution; // Screen resolution
        public Vector2 Padding;    // Alignment padding
    }

    /// <inheritdoc/>
    public override string Name => "Heat Haze";

    /// <inheritdoc/>
    public override bool IsActive => IsEnabled && Settings != null && Settings.HeatHazeEnabled;

    /// <inheritdoc/>
    public override int Priority => 150;  // After vignette, before final output

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
        EffectShader = AllShaders.PostFX_HeatHaze?.Get(0);
        if (EffectShader == null)
        {
            FLLog.Warning("PostFX", "HeatHaze shader not available, effect disabled");
            IsEnabled = false;
        }
    }

    /// <inheritdoc/>
    public override void Render(ref PostEffectContext context)
    {
        if (EffectShader == null || Settings == null)
            return;

        // Accumulate time for animation
        accumulatedTime += context.DeltaTime;
        if (accumulatedTime > 10000f)
            accumulatedTime -= 10000f;

        // Set up fullscreen pass
        var snapshot = SetupFullscreenPass(context.Output, EffectShader);

        try
        {
            float intensity = Settings.HeatHazeEnabled
                ? Math.Clamp(Settings.HeatHazeIntensity, 0f, 0.05f)
                : 0f;
            float speed = Math.Clamp(Settings.HeatHazeSpeed, 0.1f, 5f);
            float scale = Math.Clamp(Settings.HeatHazeScale, 1f, 20f);

            // Prepare shader parameters
            var parameters = new HeatHazeParams
            {
                Intensity = intensity,
                Speed = speed,
                Scale = scale,
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
