using System;
using LibreLancer.Graphics.Vertices;

namespace LibreLancer.Graphics.Backends;

interface IRenderContext
{
    int MaxSamples { get; }
    int MaxAnisotropy { get; }
    bool SupportsWireframe { get; }
    void Init(ref GraphicsState requested);
    void ApplyState(ref GraphicsState requested);
    void ApplyViewport(ref GraphicsState requested);

    void ApplyScissor(ref GraphicsState requested);

    void ApplyRenderTarget(ref GraphicsState requested);

    void ApplyShader(IShader shader);

    void Set2DState(bool cull, bool depth);

    void SetBlendMode(ushort mode);

    void ClearAll();
    void ClearColorOnly();
    void ClearDepth();
    void MemoryBarrier();

    /// <summary>
    /// Draws a fullscreen triangle without any vertex buffer.
    /// Uses SV_VertexID in the vertex shader to generate vertex positions.
    /// More efficient than a quad as it avoids diagonal seam and overdraw.
    /// </summary>
    void DrawFullscreenTriangle();

    IShader CreateShader(ReadOnlySpan<byte> program);

    IElementBuffer CreateElementBuffer(int count, bool isDynamic = false);
    IVertexBuffer CreateVertexBuffer(Type type, int length, bool isStream = false);
    IVertexBuffer CreateVertexBuffer(IVertexType type, int length, bool isStream = false);

    ITexture2D CreateTexture2D(int width, int height, bool hasMipMaps, SurfaceFormat format);

    ITextureCube CreateTextureCube(int size, bool mipMap, SurfaceFormat format);

    IDepthBuffer CreateDepthBuffer(int width, int height);
    IDepthMap CreateDepthMap(int width, int height);

    IRenderTarget2D CreateRenderTarget2D(ITexture2D texture, IDepthBuffer buffer);

    IMultisampleTarget CreateMultisampleTarget(int width, int height, int samples);

    /// <summary>
    /// Creates a G-Buffer for deferred rendering with multiple render targets.
    /// The G-Buffer contains: Position (RGBA16F), Normal (RGBA16F), Albedo (RGBA8), Material (RGBA8), Depth (32F).
    /// Requires OpenGL 3.0+ with MRT support.
    /// </summary>
    IGBuffer CreateGBuffer(int width, int height);

    IStorageBuffer CreateUniformBuffer(int size, int stride, Type type, bool streaming = false);

    /// <summary>
    /// Creates a Shader Storage Buffer Object (SSBO) for compute shader access.
    /// Requires OpenGL 4.3+ (HasFeature(GraphicsFeature.ShaderStorageBuffer) must be true).
    /// SSBOs can be larger than uniform buffers and support read/write from shaders.
    /// </summary>
    IStorageBuffer CreateShaderStorageBuffer(int size, int stride, Type type, bool streaming = false);

    void SetCamera(ICamera camera);
    void SetIdentityCamera();

    bool HasFeature(GraphicsFeature feature);

    string GetRenderer();

    void MakeCurrent(IntPtr sdlWindow);
    void SwapWindow(IntPtr sdlWindow, bool vsync, bool fullscreen);

    Point GetDrawableSize(IntPtr sdlWindow);

    void QueryFences();
}
