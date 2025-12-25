// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Graphics;

namespace LibreLancer.Render.PostProcessing;

/// <summary>
/// Base class for post-processing effects providing common functionality.
/// </summary>
public abstract class PostEffectBase : IPostEffect
{
    /// <summary>
    /// Render context for GPU operations.
    /// </summary>
    protected RenderContext RenderContext { get; private set; }

    /// <summary>
    /// Shader used by this effect.
    /// </summary>
    protected Shader EffectShader { get; set; }

    /// <summary>
    /// Current render width.
    /// </summary>
    protected int Width { get; private set; }

    /// <summary>
    /// Current render height.
    /// </summary>
    protected int Height { get; private set; }

    /// <inheritdoc/>
    public abstract string Name { get; }

    /// <inheritdoc/>
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc/>
    public virtual bool IsActive => IsEnabled;

    /// <inheritdoc/>
    public abstract int Priority { get; }

    /// <inheritdoc/>
    public virtual PostEffectInputs RequiredInputs => PostEffectInputs.Color;

    /// <inheritdoc/>
    public int FailureCount { get; set; }

    /// <inheritdoc/>
    public virtual void Initialize(RenderContext context)
    {
        RenderContext = context;
    }

    /// <inheritdoc/>
    public virtual void OnResize(int width, int height)
    {
        Width = width;
        Height = height;
    }

    /// <inheritdoc/>
    public abstract void Render(ref PostEffectContext context);

    /// <summary>
    /// Helper to set up common render state for fullscreen effects.
    /// Returns a snapshot that should be restored after rendering.
    /// </summary>
    protected RenderStateSnapshot SetupFullscreenPass(RenderTarget target, Shader shader)
    {
        var snapshot = new RenderStateSnapshot(RenderContext);

        RenderContext.RenderTarget = target;
        RenderContext.DepthEnabled = false;
        RenderContext.DepthWrite = false;
        RenderContext.Cull = false;
        RenderContext.BlendMode = BlendMode.Opaque;
        RenderContext.ColorWrite = true;
        RenderContext.Shader = shader;

        return snapshot;
    }

    /// <summary>
    /// Helper to restore render state after a fullscreen pass.
    /// </summary>
    protected void RestoreState(RenderStateSnapshot snapshot)
    {
        snapshot.Restore(RenderContext);
        RenderContext.Apply();
    }

    /// <inheritdoc/>
    public virtual void Dispose()
    {
        // Override in derived classes to dispose effect-specific resources
    }
}
