using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
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

        if ("GLES".Equals(Environment.GetEnvironmentVariable("LIBRELANCER_RENDERER"), StringComparison.OrdinalIgnoreCase) ||
            !CreateContextCore(sdlWin, out glcontext))
        {
            if (!CreateContextES(sdlWin, out glcontext))
            {
                return IntPtr.Zero;
            }
        }
        GL.LoadSDL(SDL3.Supported ? SDL3.SDL_GL_GetProcAddress : SDL2.SDL_GL_GetProcAddress);
        return glcontext;
    }

    static IntPtr SDLGL_Create(IntPtr sdlWin, int major, int minor, bool gles)
    {
        if (SDL3.Supported)
        {
            const int SDL3_CORE = 0x1;
            const int SDL3_GLES = 0x4;
            SDL3.SDL_GL_SetAttribute(SDL3.SDL_GLAttr.SDL_GL_CONTEXT_MAJOR_VERSION, major);
            SDL3.SDL_GL_SetAttribute(SDL3.SDL_GLAttr.SDL_GL_CONTEXT_MINOR_VERSION, minor);
            SDL3.SDL_GL_SetAttribute(SDL3.SDL_GLAttr.SDL_GL_CONTEXT_PROFILE_MASK, gles ? SDL3_GLES : SDL3_CORE);
        }
        else
        {
            SDL2.SDL_GL_SetAttribute(SDL2.SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, major);
            SDL2.SDL_GL_SetAttribute(SDL2.SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, minor);
            SDL2.SDL_GL_SetAttribute(SDL2.SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK,
                (int)(gles
                    ? SDL2.SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_ES
                    : SDL2.SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_CORE));
        }
        return SDL3.Supported
            ? SDL3.SDL_GL_CreateContext(sdlWin)
            : SDL2.SDL_GL_CreateContext(sdlWin);
    }

    static void SDLGL_Delete(IntPtr ctx)
    {
        if (SDL3.Supported)
            SDL3.SDL_GL_DestroyContext(ctx);
        else
            SDL2.SDL_GL_DeleteContext(ctx);
    }


    static bool CreateContextCore(IntPtr sdlWin, out IntPtr ctx)
    {
        ctx = SDLGL_Create(sdlWin, 3, 1, false);
        if (ctx == IntPtr.Zero) return false;
        if (!GL.CheckStringSDL())
        {
            SDLGL_Delete(ctx);
            ctx = IntPtr.Zero;
        }
        return true;
    }
    static bool CreateContextES(IntPtr sdlWin, out IntPtr ctx)
    {
        //mesa on raspberry pi OS won't give you a 3.1 context if you request it
        //but it will give you 3.1 if you request 3.0  ¯\_(ツ)_/¯
        ctx = SDLGL_Create(sdlWin, 3, 0, true);
        if (ctx == IntPtr.Zero) return false;
        if (!GL.CheckStringSDL(true))
        {
            SDLGL_Delete(ctx);
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

    private List<(IntPtr Fence, Action Callback)> Fences = new();

    public void AddFence(IntPtr fence, Action callback) => Fences.Add((fence, callback));

    public void QueryFences()
    {
        for (int i = 0; i < Fences.Count; i++)
        {
            var r = GL.ClientWaitSync(Fences[i].Fence, 0, 0);
            if (r == GL.GL_CONDITION_SATISFIED ||
                r == GL.GL_ALREADY_SIGNALED)
            {
                GL.DeleteSync(Fences[i].Fence);
                var cb = Fences[i].Callback;
                Fences.RemoveAt(i);
                i--;
                cb();
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct CameraMatrices
    {
        public Matrix4x4 View;
        public Matrix4x4 Projection;
        public Matrix4x4 ViewProjection;
        public Vector3 CameraPosition;
        private float _padding;
    }
    private ulong cameraTag;
    private ulong setCameraTag = 0;
    private CameraMatrices matrices;

    public void SetCamera(ICamera camera)
    {
        setCameraTag++;
        setCameraTag &= 0x3FFFFFFFFFFFFFFF; //top bit free (never ulong.MaxValue)
        cameraTag = (setCameraTag << 1) | 0x1; // cameraTag is never 0
        matrices.View = camera.View;
        matrices.Projection = camera.Projection;
        matrices.ViewProjection = camera.ViewProjection;
        matrices.CameraPosition = camera.Position;
    }

    public void SetIdentityCamera()
    {
        if (cameraTag == ulong.MaxValue)
        {
            return;
        }
        cameraTag = ulong.MaxValue;
        matrices.View = Matrix4x4.Identity;
        matrices.Projection = Matrix4x4.Identity;
        matrices.ViewProjection = Matrix4x4.Identity;
        matrices.CameraPosition = Vector3.Zero;
    }

    public void ApplyShader(IShader shader)
    {
        if (shader != null)
        {
            if(shader.HasUniformBlock(1) &&
               shader.UniformBlockTag(1) != cameraTag)
            {
                shader.UniformBlockTag(1) = cameraTag;
                shader.SetUniformBlock(1, ref matrices);
            }
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

    private int lastHeight = 768;

    public void ApplyViewport(ref GraphicsState requested)
    {
        var convVp = requested.Viewport;
        if (requested.RenderTarget == null)
        {
            convVp.Y = lastHeight - convVp.Y - convVp.Height;
        }
        if (convVp != applied.Viewport)
        {
            GL.Viewport(convVp.X, convVp.Y, convVp.Width, convVp.Height);
            applied.Viewport = convVp;
            applied.ScissorRect = new Rectangle();
        }
    }

    public void Set2DState(bool depth, bool cull)
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
    }

    private Rectangle appliedConvertedScissor = new Rectangle();

    public void ApplyScissor(ref GraphicsState requested)
    {
        if (requested.ScissorEnabled != applied.ScissorEnabled)
        {
            if (requested.ScissorEnabled) GL.Enable(GL.GL_SCISSOR_TEST);
            else GL.Disable(GL.GL_SCISSOR_TEST);
            applied.ScissorEnabled = requested.ScissorEnabled;
        }

        if (requested.ScissorEnabled)
        {
            var convVp = requested.ScissorRect;
            if (requested.RenderTarget == null)
            {
                convVp.Y = lastHeight - convVp.Y - convVp.Height;
            }
            else if (requested.RenderTarget is GLRenderTarget2D r2d)
            {
                convVp.Y = r2d.Height - convVp.Y - convVp.Height;
            }
            if (convVp != applied.ScissorRect)
            {
                if (convVp.Width < 1) convVp.Width = 1;
                if (convVp.Height < 1) convVp.Height = 1;
                GL.Scissor(convVp.X, convVp.Y, convVp.Width, convVp.Height);
                applied.ScissorRect = convVp;
            }
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

    public void ClearColorOnly()
    {
        GL.Clear(GL.GL_COLOR_BUFFER_BIT);
    }

    public void ClearDepth()
    {
        GL.Clear(GL.GL_DEPTH_BUFFER_BIT);
    }

    public void MemoryBarrier()
    {
        GL.MemoryBarrier(GL.GL_SHADER_STORAGE_BARRIER_BIT);
    }

    public void PrepareBlit(bool scissor)
    {
        applied.RenderTarget = null;
        if (!scissor && applied.ScissorEnabled)
        {
            applied.ScissorEnabled = false;
            GL.Disable(GL.GL_SCISSOR_TEST);
        }
    }

    public IRenderTarget CurrentTarget=> applied.RenderTarget;

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
        GraphicsFeature.DebugInfo => GLExtensions.DebugInfo,
        GraphicsFeature.S3TC => GLExtensions.S3TC,
        GraphicsFeature.GLES => GL.GLES,
        _ => false
    };

    public string GetRenderer() => $"OpenGL Renderer - {GL.GetString(GL.GL_VERSION)} ({GL.GetString(GL.GL_RENDERER)})";

    public void MakeCurrent(IntPtr sdlWindow)
    {
        if (SDL3.Supported)
            SDL3.SDL_GL_MakeCurrent(sdlWindow, glContext);
        else
            SDL2.SDL_GL_MakeCurrent(sdlWindow, glContext);
        lastHeight = GetDrawableSize(sdlWindow).Y;
    }

    public void SwapWindow(IntPtr sdlWindow, bool vsync, bool fullscreen)
    {
        lastHeight = GetDrawableSize(sdlWindow).Y;
        GLSwap.SwapWindow(sdlWindow, vsync, fullscreen);
        if (GL.FrameHadErrors()) //If there was a GL error, track it down.
            GL.ErrorChecking = true;
    }

    public Point GetDrawableSize(IntPtr sdlWindow)
    {
        if (SDL3.Supported)
        {
            SDL3.SDL_GetWindowSizeInPixels(sdlWindow, out  var width, out var height);
            return new Point(width, height);
        }
        else
        {
            SDL2.SDL_GL_GetDrawableSize(sdlWindow, out  var width, out var height);
            return new Point(width, height);
        }
    }

    public IShader CreateShader(ReadOnlySpan<byte> program) =>
        new GLShader(this, program);

    public IElementBuffer CreateElementBuffer(int count, bool isDynamic = false) =>
        new GLElementBuffer(this, count, isDynamic);

    public IVertexBuffer CreateVertexBuffer(Type type, int length, bool isStream = false) =>
        new GLVertexBuffer(type, length, isStream);

    public IVertexBuffer CreateVertexBuffer(IVertexType type, int length, bool isStream = false) =>
        new GLVertexBuffer(type, length, isStream);

    public IDepthBuffer CreateDepthBuffer(int width, int height) =>
        new GLDepthBuffer(width, height);

    public ITexture2D CreateTexture2D(int width, int height, bool hasMipMaps, SurfaceFormat format) =>
        new GLTexture2D(this, width, height, hasMipMaps, format);

    public ITextureCube CreateTextureCube(int size, bool mipMap, SurfaceFormat format) =>
        new GLTextureCube(size, mipMap, format);

    public IDepthMap CreateDepthMap(int width, int height) =>
        new GLDepthMap(this, width, height);

    public IRenderTarget2D CreateRenderTarget2D(ITexture2D texture, IDepthBuffer buffer) =>
        new GLRenderTarget2D(this, (GLTexture2D)texture, (GLDepthBuffer)buffer);

    public IMultisampleTarget CreateMultisampleTarget(int width, int height, int samples)
        => new GLMultisampleTarget(this, width, height, samples);

    public IStorageBuffer CreateUniformBuffer(int size, int stride, Type type, bool streaming = false)
        => new GLStorageBuffer(size, stride, type, streaming);
}
