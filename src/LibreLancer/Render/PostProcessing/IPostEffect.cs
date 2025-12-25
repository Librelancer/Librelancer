// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Graphics;

namespace LibreLancer.Render.PostProcessing;

/// <summary>
/// Interface for post-processing effects that can be chained in the rendering pipeline.
/// Effects are executed in priority order (lower priority = earlier in chain).
/// </summary>
public interface IPostEffect : IDisposable
{
    /// <summary>
    /// Display name for debug/UI purposes.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Whether this effect is currently enabled.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Whether this effect should run this frame (combines settings and availability).
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Priority for effect ordering (lower = earlier in chain).
    /// Recommended ranges: 0-99 early effects, 100-199 mid effects, 200+ late effects.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Required G-Buffer inputs. Effects requiring unavailable inputs are skipped.
    /// </summary>
    PostEffectInputs RequiredInputs { get; }

    /// <summary>
    /// Number of consecutive render failures. Used for auto-disable.
    /// </summary>
    int FailureCount { get; set; }

    /// <summary>
    /// Initialize GPU resources. Called once when effect is registered.
    /// </summary>
    /// <param name="context">Render context for resource creation.</param>
    void Initialize(RenderContext context);

    /// <summary>
    /// Handle viewport resize. Called when render dimensions change.
    /// </summary>
    /// <param name="width">New width in pixels.</param>
    /// <param name="height">New height in pixels.</param>
    void OnResize(int width, int height);

    /// <summary>
    /// Render the effect.
    /// </summary>
    /// <param name="context">Context containing input textures, output target, and timing.</param>
    void Render(ref PostEffectContext context);
}

/// <summary>
/// Flags indicating which G-Buffer inputs an effect requires.
/// Effects requiring unavailable inputs will be skipped gracefully.
/// </summary>
[Flags]
public enum PostEffectInputs
{
    /// <summary>No special inputs required.</summary>
    None = 0,

    /// <summary>Scene color texture. Always available.</summary>
    Color = 1,

    /// <summary>Scene depth texture. Deferred mode only (or with explicit depth RT).</summary>
    Depth = 2,

    /// <summary>World-space normals. Deferred mode only.</summary>
    Normals = 4,

    /// <summary>World-space position. Deferred mode only.</summary>
    Position = 8
}

/// <summary>
/// Context passed to post-processing effects containing all render inputs.
/// </summary>
public readonly struct PostEffectContext
{
    /// <summary>Input color texture from previous stage.</summary>
    public readonly Texture2D Input;

    /// <summary>Scene depth texture. May be null in forward mode.</summary>
    public readonly Texture2D Depth;

    /// <summary>G-Buffer normals texture. May be null in forward mode.</summary>
    public readonly Texture2D Normals;

    /// <summary>Target to render to.</summary>
    public readonly RenderTarget Output;

    /// <summary>Frame delta time in seconds.</summary>
    public readonly float DeltaTime;

    /// <summary>Render width in pixels.</summary>
    public readonly int Width;

    /// <summary>Render height in pixels.</summary>
    public readonly int Height;

    public PostEffectContext(
        Texture2D input,
        Texture2D depth,
        Texture2D normals,
        RenderTarget output,
        float deltaTime,
        int width,
        int height)
    {
        Input = input;
        Depth = depth;
        Normals = normals;
        Output = output;
        DeltaTime = deltaTime;
        Width = width;
        Height = height;
    }
}
