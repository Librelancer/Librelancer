using System;
using System.Numerics;
using LibreLancer.Graphics.Vertices;

namespace LibreLancer.Graphics.Backends.OpenGL;

class GLRenderContext : IRenderContext
{
    public uint NullVAO;

    private GraphicsState applied;
    private IntPtr glContext;

    public bool SupportsWireframe => !GL.GLES;

    public int MaxSamples { get; private set; }
    public int MaxAnisotropy { get; private set; }

    private GLRenderContext(IntPtr glContext) =>
        this.glContext = glContext;

    public static GLRenderContext Create(IntPtr sdlWindow)
    {
        var ptr = CreateGLContext(sdlWindow);
        if (ptr != IntPtr.Zero)
        {
            return new GLRenderContext(ptr);
        }
        return null;
    }

    static IntPtr CreateGLContext(IntPtr sdlWin)
    {
        IntPtr glcontext = IntPtr.Zero;
        if (Environment.GetEnvironmentVariable("LIBRELANCER_RENDERER") == "GLES" ||
            !CreateContextCore(sdlWin, out glcontext))
        {
            if (!CreateContextES(sdlWin, out glcontext))
            {
                return IntPtr.Zero;
            }
        }
        GL.LoadSDL();
        return glcontext;
    }


    static bool CreateContextCore(IntPtr sdlWin, out IntPtr ctx)
    {
        SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, 3);
        SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, 2);
        SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, (int)SDL.SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_CORE);
        ctx = SDL.SDL_GL_CreateContext(sdlWin);
        if (ctx == IntPtr.Zero) return false;
        if (!GL.CheckStringSDL()) {
            SDL.SDL_GL_DeleteContext(ctx);
            ctx = IntPtr.Zero;
        }
        return true;
    }
    static bool CreateContextES(IntPtr sdlWin, out IntPtr ctx)
    {
        //mesa on raspberry pi OS won't give you a 3.1 context if you request it
        //but it will give you 3.1 if you request 3.0  ¯\_(ツ)_/¯
        SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, 3);
        SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, 0);
        SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, (int)SDL.SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_ES);
        ctx = SDL.SDL_GL_CreateContext(sdlWin);
        if (ctx == IntPtr.Zero) return false;
        if (!GL.CheckStringSDL(true)) {
            SDL.SDL_GL_DeleteContext(ctx);
            ctx = IntPtr.Zero;
        }
        GL.GLES = true;
        return true;
    }
    public void Init(ref GraphicsState requested)
    {
        GL.ClearColor(0f, 0f, 0f, 1f);
        GL.Enable(GL.GL_BLEND);
        GL.Enable(GL.GL_DEPTH_TEST);
        GL.BlendFuncSeparate(GL.GL_SRC_ALPHA, GL.GL_ONE_MINUS_SRC_ALPHA, GL.GL_ONE, GL.GL_ONE_MINUS_SRC_ALPHA);
        GL.DepthFunc(GL.GL_LEQUAL);
        GL.Enable(GL.GL_CULL_FACE);
        GL.CullFace(GL.GL_BACK);
        GL.GenVertexArrays(1, out NullVAO);
        int ms;
        GL.GetIntegerv(GL.GL_MAX_SAMPLES, out ms);
        MaxSamples = ms;
        if (GLExtensions.Anisotropy)
        {
            int af;
            GL.GetIntegerv(GL.GL_MAX_TEXTURE_MAX_ANISOTROPY_EXT, out af);
            MaxAnisotropy = af;
            FLLog.Debug("GL", "Max Anisotropy: " + af);
        }
        else
        {
            MaxAnisotropy = 0;
            FLLog.Debug("GL", "Anisotropic Filter Not Supported!");
        }

        applied = new GraphicsState();
        applied.BlendEnabled = true;
        applied.ClearColor = Color4.Black;
        applied.DepthEnabled = true;
        applied.CullEnabled = true;
        applied.CullFaces = CullFaces.Back;
        applied.BlendMode = BlendMode.Normal;
        applied.ScissorEnabled = false;
        applied.DepthFunction = GL.GL_LEQUAL;
        applied.DepthRange = new Vector2(0, 1);
        applied.ColorWrite = true;
        applied.DepthWrite = true;
        requested = applied;
    }

    public void ApplyShader(IShader shader)
    {
        if (applied.Shader != shader && shader != null)
        {
            ((GLShader)shader).UseProgram();
            applied.Shader = shader;
        }
    }
    public void ApplyState(ref GraphicsState requested)
    {
        ApplyShader(requested.Shader);

        if (requested.ClearColor != applied.ClearColor)
        {
            GL.ClearColor(requested.ClearColor.R, requested.ClearColor.G, requested.ClearColor.B,
                requested.ClearColor.A);
            applied.ClearColor = requested.ClearColor;
        }

        if (requested.Wireframe != applied.Wireframe && !GL.GLES)
        {
            GL.PolygonMode(GL.GL_FRONT_AND_BACK, requested.Wireframe ? GL.GL_LINE : GL.GL_FILL);
            applied.Wireframe = requested.Wireframe;
        }

        ApplyViewport(ref requested);
        SetBlendMode(requested.BlendMode);
        if (requested.DepthRange != applied.DepthRange)
        {
            applied.DepthRange = requested.DepthRange;
            GL.DepthRange(requested.DepthRange.X, requested.DepthRange.Y);
        }

        if (requested.DepthEnabled != applied.DepthEnabled)
        {
            if (requested.DepthEnabled)
                GL.Enable(GL.GL_DEPTH_TEST);
            else
                GL.Disable(GL.GL_DEPTH_TEST);
            applied.DepthEnabled = requested.DepthEnabled;
        }


        if (requested.CullEnabled != applied.CullEnabled)
        {
            if (requested.CullEnabled)
                GL.Enable(GL.GL_CULL_FACE);
            else
                GL.Disable(GL.GL_CULL_FACE);
            applied.CullEnabled = requested.CullEnabled;
        }

        if (requested.CullFaces != applied.CullFaces)
        {
            applied.CullFaces = requested.CullFaces;
            GL.CullFace(requested.CullFaces == CullFaces.Back ? GL.GL_BACK : GL.GL_FRONT);
        }

        if (requested.ColorWrite != applied.ColorWrite)
        {
            applied.ColorWrite = requested.ColorWrite;
            if (requested.ColorWrite)
            {
                GL.ColorMask(true, true, true, true);
            }
            else
            {
                GL.ColorMask(false, false, false, false);
            }
        }

        if (requested.DepthWrite != applied.DepthWrite)
        {
            GL.DepthMask(requested.DepthWrite);
            applied.DepthWrite = requested.DepthWrite;
        }

        ApplyScissor(ref requested);
        if (requested.PolygonOffset != applied.PolygonOffset)
        {
            applied.PolygonOffset = requested.PolygonOffset;
            GL.PolygonOffset(requested.PolygonOffset.X, requested.PolygonOffset.Y);
        }
    }

    public void ApplyViewport(ref GraphicsState requested)
    {
        if (requested.Viewport != applied.Viewport)
        {
            GL.Viewport(requested.Viewport.X, requested.Viewport.Y, requested.Viewport.Width,
                requested.Viewport.Height);
            applied.Viewport = requested.Viewport;
            applied.ScissorRect = new Rectangle();
        }
    }

    public void Set2DState(bool depth, bool cull, bool scissor)
    {
        if (depth != applied.DepthEnabled)
        {
            applied.DepthEnabled = depth;
            if(depth)
                GL.Enable(GL.GL_DEPTH_TEST);
            else
                GL.Disable(GL.GL_DEPTH_TEST);
        }

        if (cull != applied.CullEnabled)
        {
            applied.CullEnabled = cull;
            if(cull)
                GL.Enable(GL.GL_CULL_FACE);
            else
                GL.Disable(GL.GL_CULL_FACE);
        }
        if (scissor != applied.ScissorEnabled)
        {
            if(scissor)
                GL.Enable(GL.GL_SCISSOR_TEST);
            else
                GL.Disable(GL.GL_SCISSOR_TEST);
        }
    }

    public void ApplyScissor(ref GraphicsState requested)
    {
        if (requested.ScissorEnabled != applied.ScissorEnabled)
        {
            if (requested.ScissorEnabled) GL.Enable(GL.GL_SCISSOR_TEST);
            else GL.Disable(GL.GL_SCISSOR_TEST);
            applied.ScissorEnabled = requested.ScissorEnabled;
        }

        if (requested.ScissorEnabled & (requested.ScissorRect != applied.ScissorRect))
        {
            var cr = requested.ScissorRect;
            applied.ScissorRect = cr;
            if (cr.Height < 1) cr.Height = 1;
            if (cr.Width < 1) cr.Width = 1;
            GL.Scissor(cr.X, applied.Viewport.Height - cr.Y - cr.Height, cr.Width, cr.Height);
        }
    }

    private static readonly int[] BlendTable =
    {
        0,
        GL.GL_ZERO,
        GL.GL_ONE,
        GL.GL_SRC_COLOR,
        GL.GL_ONE_MINUS_SRC_COLOR,
        GL.GL_SRC_ALPHA,
        GL.GL_ONE_MINUS_SRC_ALPHA,
        GL.GL_DST_ALPHA,
        GL.GL_ONE_MINUS_DST_ALPHA,
        GL.GL_DST_COLOR,
        GL.GL_ONE_MINUS_DST_COLOR,
        GL.GL_SRC_ALPHA_SATURATE
    };

    public void SetBlendMode(ushort mode)
    {
        if (mode != applied.BlendMode)
        {
            BlendMode.Validate(mode);
            if (!applied.BlendEnabled && mode != BlendMode.Opaque)
            {
                GL.Enable(GL.GL_BLEND);
                applied.BlendEnabled = true;
            }

            if (applied.BlendEnabled && mode == BlendMode.Opaque)
            {
                GL.Disable(GL.GL_BLEND);
                applied.BlendEnabled = false;
            }

            GL.BlendFuncSeparate(BlendTable[(mode >> 8) & 0xFF],BlendTable[(mode & 0xFF)], GL.GL_ONE, GL.GL_ONE_MINUS_SRC_ALPHA);
            applied.BlendMode = mode;
        }
    }

    public void ClearAll()
    {
        GL.Clear(GL.GL_COLOR_BUFFER_BIT | GL.GL_DEPTH_BUFFER_BIT);
    }

    public void ClearDepth()
    {
        GL.Clear(GL.GL_DEPTH_BUFFER_BIT);
    }

    public void MemoryBarrier()
    {
        GL.MemoryBarrier(GL.GL_SHADER_STORAGE_BARRIER_BIT);
    }

    public void PrepareBlit()
    {
        applied.RenderTarget = null;
        if (applied.ScissorEnabled)
        {
            applied.ScissorEnabled = false;
            GL.Disable(GL.GL_SCISSOR_TEST);
        }
    }

    public void ApplyRenderTarget(ref GraphicsState requested)
    {
        if (applied.RenderTarget != requested.RenderTarget)
        {
            applied.RenderTarget = requested.RenderTarget;
            if(requested.RenderTarget != null)
                ((GLRenderTarget)requested.RenderTarget).BindFramebuffer();
            else
                GL.BindFramebuffer(GL.GL_FRAMEBUFFER, 0);
        }
    }

    public bool HasFeature(GraphicsFeature feature) => feature switch
    {
        GraphicsFeature.Anisotropy => GLExtensions.Anisotropy,
        GraphicsFeature.Features430 => GLExtensions.Features430,
        GraphicsFeature.ComputeShaders => GLExtensions.ComputeShaders,
        GraphicsFeature.DebugInfo => GLExtensions.DebugInfo,
        GraphicsFeature.S3TC => GLExtensions.S3TC,
        GraphicsFeature.GLES => GL.GLES,
        _ => false
    };

    public string GetRenderer() => $"OpenGL Renderer - {GL.GetString(GL.GL_VERSION)} ({GL.GetString(GL.GL_RENDERER)})";

    public void MakeCurrent(IntPtr sdlWindow)
    {
        SDL.SDL_GL_MakeCurrent(sdlWindow, glContext);
    }

    public void SwapWindow(IntPtr sdlWindow, bool vsync, bool fullscreen)
    {
        GLSwap.SwapWindow(sdlWindow, vsync, fullscreen);
        if (GL.FrameHadErrors()) //If there was a GL error, track it down.
            GL.ErrorChecking = true;
    }

    public Point GetDrawableSize(IntPtr sdlWindow)
    {
        SDL.SDL_GL_GetDrawableSize(sdlWindow, out  var width, out var height);
        return new Point(width, height);
    }

    public IShader CreateShader(string vertex_source, string fragment_source, string geometry_source = null) =>
        new GLShader(this, vertex_source, fragment_source, geometry_source);

    public IElementBuffer CreateElementBuffer(int count, bool isDynamic = false) =>
        new GLElementBuffer(this, count, isDynamic);

    public IVertexBuffer CreateVertexBuffer(Type type, int length, bool isStream = false) =>
        new GLVertexBuffer(type, length, isStream);

    public IVertexBuffer CreateVertexBuffer(IVertexType type, int length, bool isStream = false) =>
        new GLVertexBuffer(type, length, isStream);

    public IComputeShader CreateComputeShader(string shaderCode) =>
        new GLComputeShader(shaderCode);

    public IDepthBuffer CreateDepthBuffer(int width, int height) =>
        new GLDepthBuffer(width, height);

    public ITexture2D CreateTexture2D(int width, int height, bool hasMipMaps, SurfaceFormat format) =>
        new GLTexture2D(width, height, hasMipMaps, format);

    public ITextureCube CreateTextureCube(int size, bool mipMap, SurfaceFormat format) =>
        new GLTextureCube(size, mipMap, format);

    public IDepthMap CreateDepthMap(int width, int height) =>
        new GLDepthMap(width, height);

    public IRenderTarget2D CreateRenderTarget2D(ITexture2D texture, IDepthBuffer buffer) =>
        new GLRenderTarget2D(this, (GLTexture2D)texture, (GLDepthBuffer)buffer);

    public IShaderStorageBuffer CreateShaderStorageBuffer(int size)
        => new GLShaderStorageBuffer(size);

    public IMultisampleTarget CreateMultisampleTarget(int width, int height, int samples)
        => new GLMultisampleTarget(this, width, height, samples);

    public IUniformBuffer CreateUniformBuffer(int size, int stride, Type type, bool streaming = false)
        => new GLUniformBuffer(size, stride, type, streaming);
}
