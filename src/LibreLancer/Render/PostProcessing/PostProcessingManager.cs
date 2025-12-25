// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Graphics;

namespace LibreLancer.Render.PostProcessing;

/// <summary>
/// Manages post-processing effects pipeline with ping-pong buffer strategy.
/// Handles effect registration, ordering, and execution with error isolation.
/// </summary>
public class PostProcessingManager : IDisposable
{
    private readonly RenderContext rstate;
    private readonly List<IPostEffect> effects = new();
    private readonly List<IPostEffect> activeEffectsCache = new();  // Reusable list to avoid allocations

    // Ping-pong buffers for effect chaining
    private RenderTarget2D resolvedScene;  // MSAA resolve target
    private RenderTarget2D bufferA;
    private RenderTarget2D bufferB;

    private int width;
    private int height;
    private bool initialized;
    private bool disposed;

    // Toggle debouncing
    private double lastToggleTime;
    private const double DebounceInterval = 0.2;  // 200ms

    // Error handling
    private const int MaxFailuresBeforeDisable = 3;

    /// <summary>
    /// Global post-processing settings.
    /// </summary>
    public PostProcessingSettings Settings { get; } = new();

    /// <summary>
    /// Whether G-Buffer data is available (deferred mode active).
    /// Set by SystemRenderer before calling Render().
    /// </summary>
    public bool HasGBuffer { get; set; }

    /// <summary>
    /// Access to deferred renderer's G-Buffer for texture binding.
    /// Set by SystemRenderer if deferred mode is active.
    /// </summary>
    public GBuffer GBuffer { get; set; }

    public PostProcessingManager(RenderContext rstate)
    {
        this.rstate = rstate;
    }

    /// <summary>
    /// Register a post-processing effect.
    /// Effects are automatically sorted by priority.
    /// </summary>
    public void RegisterEffect(IPostEffect effect)
    {
        effects.Add(effect);
        effects.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        if (initialized)
        {
            effect.Initialize(rstate);
            effect.OnResize(width, height);
        }
    }

    /// <summary>
    /// Initialize all registered effects.
    /// </summary>
    public void Initialize()
    {
        if (initialized) return;
        initialized = true;

        foreach (var effect in effects)
        {
            effect.Initialize(rstate);
        }
    }

    /// <summary>
    /// Handle viewport resize.
    /// </summary>
    public void Resize(int newWidth, int newHeight)
    {
        if (newWidth <= 0 || newHeight <= 0) return;
        if (width == newWidth && height == newHeight) return;

        width = newWidth;
        height = newHeight;

        // Dispose old buffers - they'll be recreated lazily
        DisposeBuffers();

        foreach (var effect in effects)
        {
            effect.OnResize(width, height);
        }
    }

    /// <summary>
    /// Toggle master effects with debouncing.
    /// Returns true if toggle was applied.
    /// </summary>
    public bool TryToggle(double currentTime)
    {
        if (currentTime - lastToggleTime < DebounceInterval)
            return false;

        lastToggleTime = currentTime;
        Settings.EffectsEnabled = !Settings.EffectsEnabled;

        var state = Settings.EffectsEnabled ? "ON" : "OFF";
        FLLog.Info("PostFX", $"Post-processing: {state}");

        return true;
    }

    /// <summary>
    /// Ensure ping-pong buffers exist.
    /// Returns false if allocation fails.
    /// </summary>
    private bool EnsureBuffers()
    {
        try
        {
            resolvedScene ??= new RenderTarget2D(rstate, width, height);
            bufferA ??= new RenderTarget2D(rstate, width, height);
            bufferB ??= new RenderTarget2D(rstate, width, height);
            return true;
        }
        catch (Exception ex)
        {
            FLLog.Error("PostFX", $"Buffer allocation failed: {ex.Message}");
            Settings.EffectsEnabled = false;  // Disable PostFX on failure
            DisposeBuffers();
            return false;
        }
    }

    /// <summary>
    /// Dispose ping-pong buffers.
    /// </summary>
    private void DisposeBuffers()
    {
        resolvedScene?.Dispose();
        resolvedScene = null;
        bufferA?.Dispose();
        bufferA = null;
        bufferB?.Dispose();
        bufferB = null;
    }

    /// <summary>
    /// Check if an effect can run with current available inputs.
    /// </summary>
    private bool CanRunEffect(IPostEffect effect)
    {
        var required = effect.RequiredInputs;

        // Check for deferred-only inputs
        if ((required & PostEffectInputs.Normals) != 0 && !HasGBuffer)
            return false;
        if ((required & PostEffectInputs.Position) != 0 && !HasGBuffer)
            return false;
        if ((required & PostEffectInputs.Depth) != 0 && !HasGBuffer)
            return false;

        return true;
    }

    /// <summary>
    /// Get list of active effects that can run with current inputs.
    /// Reuses a cached list to avoid per-frame allocations.
    /// </summary>
    private List<IPostEffect> GetActiveEffects()
    {
        activeEffectsCache.Clear();
        foreach (var effect in effects)
        {
            if (effect.IsActive && CanRunEffect(effect))
            {
                activeEffectsCache.Add(effect);
            }
        }
        return activeEffectsCache;
    }

    /// <summary>
    /// Execute the post-processing chain.
    /// </summary>
    /// <param name="sceneColor">Resolved scene color texture (from MSAA resolve or current target).</param>
    /// <param name="sceneDepth">Scene depth texture. May be null in forward mode.</param>
    /// <param name="normals">G-Buffer normals. May be null in forward mode.</param>
    /// <param name="finalTarget">Final output target (typically screen or restoreTarget).</param>
    /// <param name="deltaTime">Frame delta time in seconds.</param>
    /// <returns>True if any effects were applied.</returns>
    public bool Render(
        Texture2D sceneColor,
        Texture2D sceneDepth,
        Texture2D normals,
        RenderTarget finalTarget,
        float deltaTime)
    {
        if (!Settings.EffectsEnabled)
            return false;

        if (sceneColor == null)
            return false;

        var activeEffects = GetActiveEffects();
        if (activeEffects.Count == 0)
            return false;

        if (!EnsureBuffers())
            return false;

        Texture2D currentInput = sceneColor;
        RenderTarget currentOutput;
        bool useBufferA = true;

        for (int i = 0; i < activeEffects.Count; i++)
        {
            var effect = activeEffects[i];
            bool isLast = (i == activeEffects.Count - 1);

            // Last effect writes to final target, others alternate buffers
            currentOutput = isLast ? finalTarget : (useBufferA ? bufferA : bufferB);

            var context = new PostEffectContext(
                currentInput,
                sceneDepth,
                normals,
                currentOutput,
                deltaTime,
                width,
                height
            );

            try
            {
                effect.Render(ref context);
                effect.FailureCount = 0;  // Reset on success
            }
            catch (Exception ex)
            {
                effect.FailureCount++;
                FLLog.Error("PostFX", $"{effect.Name} failed: {ex.Message}");

                if (effect.FailureCount >= MaxFailuresBeforeDisable)
                {
                    effect.IsEnabled = false;
                    FLLog.Warning("PostFX", $"{effect.Name} auto-disabled after {MaxFailuresBeforeDisable} failures");
                }

                // Skip this effect and continue with chain
                continue;
            }

            // Update input for next effect (if not last)
            if (!isLast)
            {
                currentInput = useBufferA ? bufferA.Texture : bufferB.Texture;
                useBufferA = !useBufferA;
            }
        }

        return true;
    }

    /// <summary>
    /// Get the resolved scene buffer for MSAA resolve.
    /// Returns null if buffers aren't ready.
    /// </summary>
    public RenderTarget2D GetResolveTarget()
    {
        if (!Settings.EffectsEnabled)
            return null;

        if (width <= 0 || height <= 0)
            return null;

        if (!EnsureBuffers())
            return null;

        return resolvedScene;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;

        foreach (var effect in effects)
        {
            try
            {
                effect.Dispose();
            }
            catch (Exception ex)
            {
                FLLog.Warning("PostFX", $"Error disposing {effect.Name}: {ex.Message}");
            }
        }
        effects.Clear();

        DisposeBuffers();
    }
}
