using System;
using System.Numerics;
using LibreLancer.Graphics.Backends.OpenGL;
using LibreLancer.Graphics.Vertices;

namespace LibreLancer.Graphics.Backends.Null;

class NullRenderContext : IRenderContext
{
    public int MaxSamples => 0;
    public int MaxAnisotropy => 0;
    public bool SupportsWireframe => false;
    public void Init(ref GraphicsState requested)
    {
        FLLog.Info("Graphics", "NULL backend requested");
        requested.BlendEnabled = true;
        requested.ClearColor = Color4.Black;
        requested.DepthEnabled = true;
        requested.CullEnabled = true;
        requested.CullFaces = CullFaces.Back;
        requested.BlendMode = BlendMode.Normal;
        requested.ScissorEnabled = false;
        requested.DepthFunction = GL.GL_LEQUAL;
        requested.DepthRange = new Vector2(0, 1);
        requested.ColorWrite = true;
        requested.DepthWrite = true;
    }

    public void ApplyState(ref GraphicsState requested)
    {
    }

    public void ApplyViewport(ref GraphicsState requested)
    {
    }

    public void ApplyScissor(ref GraphicsState requested)
    {
    }

    public void ApplyRenderTarget(ref GraphicsState requested)
    {
    }

    public void Set2DState(bool cull, bool depth, bool scissor)
    {
    }

    public void SetBlendMode(ushort mode)
    {
    }

    public void ClearAll()
    {
    }

    public void ClearDepth()
    {
    }

    public void MemoryBarrier()
    {
    }

    public void ApplyShader(IShader shader)
    {
    }

    public IShader CreateShader(string vertex_source, string fragment_source, string geometry_source = null) =>
        new NullShader();

    public IElementBuffer CreateElementBuffer(int count, bool isDynamic = false) =>
        new NullElementBuffer(count);

    public IVertexBuffer CreateVertexBuffer(Type type, int length, bool isStream = false) =>
        new NullVertexBuffer(type, length, isStream);

    public IVertexBuffer CreateVertexBuffer(IVertexType type, int length, bool isStream = false) =>
        new NullVertexBuffer(type, length, isStream);

    public ITexture2D CreateTexture2D(int width, int height, bool hasMipMaps, SurfaceFormat format) =>
        new NullTexture2D(width, height, hasMipMaps, format, 1);

    public ITextureCube CreateTextureCube(int size, bool mipMap, SurfaceFormat format) =>
        new NullTextureCube(size, format, 1, 256 * 256 * 6);

    public IComputeShader CreateComputeShader(string shaderCode) =>
        throw new PlatformNotSupportedException("Features430 not available in null backend");


    public IDepthBuffer CreateDepthBuffer(int width, int height) =>
        new NullDepthBuffer();

    public IDepthMap CreateDepthMap(int width, int height) =>
        new NullDepthMap(width, height);

    public IRenderTarget2D CreateRenderTarget2D(ITexture2D texture, IDepthBuffer buffer) =>
        new NullRenderTarget2D(texture.Width, texture.Height);

    public IShaderStorageBuffer CreateShaderStorageBuffer(int size) =>
        throw new PlatformNotSupportedException("Features430 not available in null backend");

    public IMultisampleTarget CreateMultisampleTarget(int width, int height, int samples) =>
        new NullMultisampleTarget(width, height);

    public IUniformBuffer CreateUniformBuffer(int size, int stride, Type type, bool streaming = false) =>
        new NullUniformBuffer(size, stride);

    public bool HasFeature(GraphicsFeature feature) => false;

    public string GetRenderer() => "NULL";
    public void MakeCurrent(IntPtr sdlWindow)
    {
    }

    public unsafe void SwapWindow(IntPtr sdlWindow, bool vsync, bool fullscreen)
    {
        SDL.SDL_Surface* window_surface = (SDL.SDL_Surface*)SDL.SDL_GetWindowSurface(sdlWindow);
        var r = new SDL.SDL_Rect();
        r.x = 0;
        r.y = 0;
        r.w = window_surface->w;
        r.h = window_surface->h;
        SDL.SDL_FillRect((IntPtr)window_surface, ref r, 0xFFFF0000);
        SDL.SDL_UpdateWindowSurface(sdlWindow);
    }

    public Point GetDrawableSize(IntPtr sdlWindow)
    {
        SDL.SDL_GetWindowSize(sdlWindow, out int windowWidth, out int windowHeight);
        return new Point(windowWidth, windowHeight);
    }
}
