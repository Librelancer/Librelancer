// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.Render.PostProcessing;

/// <summary>
/// Global settings for post-processing effects.
/// Defaults OFF on launch, not persisted across sessions.
/// </summary>
public class PostProcessingSettings
{
    /// <summary>
    /// Master toggle for all effects (CTRL+K).
    /// When false, no post-processing is applied.
    /// </summary>
    public bool EffectsEnabled { get; set; } = false;

    // Per-effect toggles (for future per-effect control)

    /// <summary>
    /// Enable vignette effect (darkened corners).
    /// </summary>
    public bool VignetteEnabled { get; set; } = true;

    /// <summary>
    /// Enable film grain effect (subtle noise overlay).
    /// </summary>
    public bool FilmGrainEnabled { get; set; } = true;

    /// <summary>
    /// Enable heat haze distortion effect.
    /// </summary>
    public bool HeatHazeEnabled { get; set; } = false;  // Temporarily disabled

    /// <summary>
    /// Enable SSAO effect (requires deferred rendering).
    /// </summary>
    public bool SSAOEnabled { get; set; } = true;

    // Vignette parameters

    /// <summary>
    /// Vignette intensity. Higher = more darkening at corners.
    /// Range: 0.0 (off) to 2.0 (strong). Default: 0.5
    /// </summary>
    public float VignetteIntensity { get; set; } = 0.5f;

    /// <summary>
    /// Vignette softness. Higher = more gradual falloff.
    /// Range: 0.1 (sharp) to 1.0 (soft). Default: 0.6
    /// </summary>
    public float VignetteSoftness { get; set; } = 0.6f;

    // Film grain parameters

    /// <summary>
    /// Film grain intensity. Higher = more visible noise.
    /// Range: 0.0 (off) to 0.2 (strong). Default: 0.05
    /// </summary>
    public float FilmGrainIntensity { get; set; } = 0.05f;

    // Heat haze parameters

    /// <summary>
    /// Heat haze distortion intensity.
    /// Range: 0.0 (off) to 0.05 (strong). Default: 0.002
    /// </summary>
    public float HeatHazeIntensity { get; set; } = 0.002f;

    /// <summary>
    /// Heat haze animation speed.
    /// Range: 0.1 (slow) to 5.0 (fast). Default: 1.0
    /// </summary>
    public float HeatHazeSpeed { get; set; } = 1.0f;

    /// <summary>
    /// Heat haze noise scale. Higher = larger distortion waves.
    /// Range: 1.0 (fine) to 20.0 (coarse). Default: 8.0
    /// </summary>
    public float HeatHazeScale { get; set; } = 8.0f;

    // SSAO parameters (using GTAO algorithm internally)

    /// <summary>
    /// World-space sample radius for ambient occlusion.
    /// Range: 0.5 (tight) to 10.0 (wide). Default: 2.0
    /// Values below 0.5 produce no visible AO; above 10.0 causes halo artifacts.
    /// </summary>
    public float SSAORadius { get; set; } = 2.0f;

    /// <summary>
    /// Distance falloff exponent. Higher = faster falloff with distance.
    /// Range: 0.5 to 3.0. Default: 1.0
    /// </summary>
    public float SSAOFalloff { get; set; } = 1.0f;

    /// <summary>
    /// AO strength multiplier. Higher = darker occlusion.
    /// Range: 0.5 (subtle) to 3.0 (strong). Default: 1.5
    /// </summary>
    public float SSAOIntensity { get; set; } = 1.5f;

    /// <summary>
    /// Number of horizon search directions (affects quality vs performance).
    /// Range: 2 to 8. Default: 4. Each +2 adds ~0.5ms.
    /// </summary>
    public int SSAODirections { get; set; } = 4;

    /// <summary>
    /// Steps per direction for horizon search.
    /// Range: 2 to 8. Default: 4. Higher = better thin feature handling.
    /// </summary>
    public int SSAOSteps { get; set; } = 4;

    /// <summary>
    /// Enable bilateral blur pass to reduce noise while preserving edges.
    /// </summary>
    public bool SSAOBlurEnabled { get; set; } = true;

    /// <summary>
    /// Depth threshold for blur edge detection. Lower values preserve more edges.
    /// Range: 0.01 (sharp edges) to 1.0 (smooth). Default: 0.1
    /// </summary>
    public float SSAOBlurDepthThreshold { get; set; } = 0.1f;
}
