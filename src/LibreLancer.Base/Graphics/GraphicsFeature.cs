namespace LibreLancer.Graphics;

public enum GraphicsFeature
{
    GLES,
    Anisotropy,
    S3TC,
    DebugInfo,
    /// <summary>
    /// OpenGL 4.3+ compute shaders (glDispatchCompute, compute shader stage)
    /// </summary>
    ComputeShaders,
    /// <summary>
    /// OpenGL 4.3+ shader storage buffer objects (SSBOs for large read/write buffers)
    /// </summary>
    ShaderStorageBuffer,
    /// <summary>
    /// OpenGL 4.6 features (SPIR-V, direct state access, no-error context)
    /// </summary>
    OpenGL46,
}
