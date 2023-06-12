// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;

namespace LibreLancer
{
	public enum DepthFunction
	{
		Less = 0x201,
		LessEqual = 0x203
	}

	//OpenGL Render States
	public class RenderContext
    {

        public long FrameNumber => frameNumber;
        private long frameNumber = 0;
        
		static internal RenderContext Instance;

        private UniformBuffer cameraBuffer;

        struct CameraMatrices
        {
            public Matrix4x4 View;
            public Matrix4x4 Projection;
            public Matrix4x4 ViewProjection;
            public Vector3 CameraPosition;
            public float Padding;
        }
        
        public static bool GLES => GL.GLES;
		public Color4 ClearColor
        {
            get => requested.ClearColor;
            set => requested.ClearColor = value;
        }

		public bool Wireframe
        {
            get => requested.Wireframe;
            set => requested.Wireframe = value;
        }

		public bool DepthEnabled
        {
            get => requested.DepthEnabled;
            set => requested.DepthEnabled = value;
        }
        TextureFiltering _preferred = TextureFiltering.Trilinear;
        public TextureFiltering PreferredFilterLevel
        {
            get { return _preferred;  }
            set {
                if (value == TextureFiltering.Anisotropic && MaxAnisotropy == 0)
                    _preferred = TextureFiltering.Trilinear;
                else
                    _preferred = value;
            }
        }
        public int MaxAnisotropy {
            get; private set;
        }
        public int MaxSamples {
            get; private set;
        }
        int _anisotropyLevel = 0;
        public int AnisotropyLevel {
            get {
                return _anisotropyLevel;
            } set {
                _anisotropyLevel = value;
            }
        }
        public int[] GetAnisotropyLevels()
        {
            if (MaxAnisotropy == 0) return null;
            var levels = new List<int>();
            int i = 2;
            while(i <= MaxAnisotropy) {
                levels.Add(i);
                i *= 2;
            }
            return levels.ToArray();
        }
        
        public bool DepthWrite
        {
            get => requested.DepthWrite;
            set => requested.DepthWrite = value;
        }
		public BlendMode BlendMode
        {
            get => requested.BlendMode;
            set => requested.BlendMode = value;
        }

		public DepthFunction DepthFunction
        {
            get => (DepthFunction) requested.DepthFunction;
            set => requested.DepthFunction = (int) value;
        }

        public Vector2 DepthRange
        {
            get => requested.DepthRange;
            set => requested.DepthRange = value;
        }

        public bool ColorWrite
        {
            get => requested.ColorWrite;
            set => requested.ColorWrite = value;
        }
		public bool Cull
        {
            get => requested.CullEnabled;
            set => requested.CullEnabled = value;
        }

        public bool ScissorEnabled
        {
            get => requested.ScissorEnabled;
            set
            {
                Renderer2D.ScissorChanged();
                requested.ScissorEnabled = value;
            }
        }

        public Rectangle ScissorRectangle
        {
            get => requested.ScissorRect;
            set
            {
                Renderer2D.ScissorChanged();
                requested.ScissorRect = value;
            }
        }

        public RenderTarget RenderTarget
        {
            get => requested.RenderTarget;
            set
            {
                Renderer2D.Flush();
                requested.RenderTarget = value;
            }
        }

        public CullFaces CullFace
        {
            get => requested.CullFaces;
            set => requested.CullFaces = value;
        }

		internal void Trash()
		{
			
		}

        private bool cameraIsIdentity = false;

        public void SetIdentityCamera()
        {
            if (cameraIsIdentity) return;
            var matrices = new CameraMatrices();
            matrices.View = Matrix4x4.Identity;
            matrices.Projection = Matrix4x4.Identity;
            matrices.ViewProjection = Matrix4x4.Identity;
            matrices.CameraPosition = Vector3.Zero;
            cameraBuffer.SetData(ref matrices);
            cameraIsIdentity = true;
        }
        public void SetCamera(ICamera camera)
        {
            var matrices = new CameraMatrices();
            matrices.View = camera.View;
            matrices.Projection = camera.Projection;
            matrices.ViewProjection = camera.ViewProjection;
            matrices.CameraPosition = camera.Position;
            cameraBuffer.SetData(ref matrices);
            cameraIsIdentity = false;
        }

        public uint NullVAO;
        
        public Renderer2D Renderer2D { get; }

        public RenderContext ()
		{
			GL.ClearColor (0f, 0f, 0f, 1f);
			GL.Enable (GL.GL_BLEND);
			GL.Enable (GL.GL_DEPTH_TEST);
			GL.BlendFunc (GL.GL_SRC_ALPHA, GL.GL_ONE_MINUS_SRC_ALPHA);
			GL.DepthFunc (GL.GL_LEQUAL);
			GL.Enable (GL.GL_CULL_FACE);
			GL.CullFace (GL.GL_BACK);
            GL.GenVertexArrays(1, out NullVAO);
			Instance = this;
			PreferredFilterLevel = TextureFiltering.Trilinear;
            int ms;
            GL.GetIntegerv(GL.GL_MAX_SAMPLES, out ms);
            MaxSamples = ms;
            if(GLExtensions.Anisotropy) {
                int af;
                GL.GetIntegerv(GL.GL_MAX_TEXTURE_MAX_ANISOTROPY_EXT, out af);
                MaxAnisotropy = af;
                FLLog.Debug("GL", "Max Anisotropy: " + af);
            } else {
                MaxAnisotropy = 0;
                FLLog.Debug("GL", "Anisotropic Filter Not Supported!");
            }
            applied = new GLState();
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
            Renderer2D = new Renderer2D(this);
            cameraBuffer = new UniformBuffer(1, Marshal.SizeOf<CameraMatrices>(), typeof(CameraMatrices));
            SetIdentityCamera();
            cameraBuffer.BindTo(2);
        }

        public Rectangle CurrentViewport => requested.Viewport;

        Stack<Rectangle> viewports = new Stack<Rectangle>();
        
        public void PushViewport(int x, int y, int width, int height)
        {
            var vp = new Rectangle (x, y, width, height);
            viewports.Push (vp);
            SetViewport(new Rectangle(x,y,width,height));
        }
        
        public void ReplaceViewport(int x, int y, int width, int height)
        {
            if(viewports.Count >= 1)
                viewports.Pop ();
            PushViewport (x, y, width, height);
        }
        
        public void PopViewport()
        {
            viewports.Pop ();
            if (viewports.Count > 0) {
                var vp = viewports.Peek();
                SetViewport(new Rectangle(vp.X, vp.Y, vp.Width, vp.Height));
            }
        }

        void SetViewport(Rectangle vp)
        {
            Renderer2D.Flush();
            requested.Viewport = vp;
        }

        public void Flush() => Renderer2D.Flush();

        public Vector2 PolygonOffset
        {
            get => requested.PolygonOffset;
            set => requested.PolygonOffset = value;
        }

        public void SSBOMemoryBarrier()
        {
            GL.MemoryBarrier(GL.GL_SHADER_STORAGE_BARRIER_BIT);
        }

        public void ClearAll()
		{
			Apply();
			GL.Clear (GL.GL_COLOR_BUFFER_BIT | GL.GL_DEPTH_BUFFER_BIT);
            frameNumber++;
        }

		public void ClearDepth()
		{
            Apply();
            GL.Clear (GL.GL_DEPTH_BUFFER_BIT);
		}

        internal struct GLState
        {
            public bool DepthEnabled;
            public RenderTarget RenderTarget;
            public bool Wireframe;
            public Color4 ClearColor;
            public BlendMode BlendMode;
            public bool BlendEnabled;
            public bool CullEnabled;
            public CullFaces CullFaces;
            public bool ColorWrite;
            public bool DepthWrite;
            public bool ScissorEnabled;
            public Rectangle ScissorRect;
            public int DepthFunction;
            public Vector2 DepthRange;
            public Rectangle Viewport;
            public Vector2 PolygonOffset;
        }
        internal GLState applied;
        private GLState requested;

        internal void ApplyViewport()
        {
            if (requested.Viewport != applied.Viewport)
            {
                GL.Viewport(requested.Viewport.X, requested.Viewport.Y, requested.Viewport.Width,
                    requested.Viewport.Height);
                applied.Viewport = requested.Viewport;
                applied.ScissorRect = new Rectangle();
            }
        }
        
        internal void EndFrame()
        {
            Renderer2D.Flush();
            if (viewports.Count != 1)
                throw new Exception ("viewports.Count != 1 at end of frame");
        }

        internal void ApplyScissor()
        {
            if(requested.ScissorEnabled != applied.ScissorEnabled)
            {
                if (requested.ScissorEnabled) GL.Enable(GL.GL_SCISSOR_TEST);
                else GL.Disable(GL.GL_SCISSOR_TEST);
                applied.ScissorEnabled = requested.ScissorEnabled;
            }
            if(requested.ScissorEnabled & (requested.ScissorRect != applied.ScissorRect))
            {
                var cr = requested.ScissorRect;
                applied.ScissorRect = cr;
                if (cr.Height < 1) cr.Height = 1;
                if (cr.Width < 1) cr.Width = 1;
                GL.Scissor(cr.X, applied.Viewport.Height - cr.Y - cr.Height, cr.Width, cr.Height);
            }
        }

        internal void ApplyRenderTarget()
        {
            if (requested.RenderTarget != applied.RenderTarget) {
                applied.RenderTarget = RenderTarget;
                if (RenderTarget != null) RenderTarget.BindFramebuffer();
                else LibreLancer.RenderTarget.ClearBinding();
            }
        }

        internal void SetBlendMode(BlendMode mode)
        {
            if (mode != applied.BlendMode) {
                if (!applied.BlendEnabled && mode != BlendMode.Opaque) {
                    GL.Enable (GL.GL_BLEND);
                    applied.BlendEnabled = true;
                }
                if (applied.BlendEnabled && mode == BlendMode.Opaque) {
                    GL.Disable (GL.GL_BLEND);
                    applied.BlendEnabled = false;
                }
                switch (mode)
                {
                    case BlendMode.Normal:
                        GL.BlendFunc(GL.GL_SRC_ALPHA, GL.GL_ONE_MINUS_SRC_ALPHA);
                        break;
                    case BlendMode.Additive:
                        GL.BlendFunc(GL.GL_SRC_ALPHA, GL.GL_ONE);
                        break;
                    case BlendMode.OneInvSrcColor:
                        GL.BlendFunc (GL.GL_ONE, GL.GL_ONE_MINUS_SRC_COLOR);
                        break;
                    case BlendMode.SrcAlphaInvDestColor:
                        GL.BlendFunc(GL.GL_SRC_ALPHA, GL.GL_ONE_MINUS_DST_COLOR);
                        break;
                    case BlendMode.InvDestColorSrcAlpha:
                        GL.BlendFunc(GL.GL_ONE_MINUS_DST_COLOR, GL.GL_SRC_ALPHA);
                        break;
                    case BlendMode.DestColorSrcColor:
                        GL.BlendFunc(GL.GL_DST_COLOR, GL.GL_SRC_COLOR);
                        break;
                }
                applied.BlendMode = mode;
            }
        }
		public void Apply()
		{
            Renderer2D.Flush();
            if (requested.ClearColor != applied.ClearColor) {
                GL.ClearColor (requested.ClearColor.R, requested.ClearColor.G, requested.ClearColor.B, requested.ClearColor.A);
                applied.ClearColor = requested.ClearColor;
            }
            if (requested.Wireframe != applied.Wireframe && !GL.GLES) {
				GL.PolygonMode (GL.GL_FRONT_AND_BACK, requested.Wireframe ? GL.GL_LINE : GL.GL_FILL);
                applied.Wireframe = requested.Wireframe;
            }

            ApplyRenderTarget();
            ApplyViewport();
            SetBlendMode(requested.BlendMode);
            if (requested.DepthRange != applied.DepthRange)
            {
                applied.DepthRange = requested.DepthRange;
                GL.DepthRange(requested.DepthRange.X, requested.DepthRange.Y);
            }
            
			if (requested.DepthEnabled != applied.DepthEnabled) {
				if (requested.DepthEnabled)
					GL.Enable (GL.GL_DEPTH_TEST);
				else
					GL.Disable (GL.GL_DEPTH_TEST);
				applied.DepthEnabled = requested.DepthEnabled;
			}

			
			if (requested.CullEnabled != applied.CullEnabled) {
				if (requested.CullEnabled)
					GL.Enable (GL.GL_CULL_FACE);
				else
					GL.Disable (GL.GL_CULL_FACE);
                applied.CullEnabled = requested.CullEnabled;
            }
			if (requested.CullFaces != applied.CullFaces)
			{
				applied.CullFaces = requested.CullFaces;
				GL.CullFace(requested.CullFaces == CullFaces.Back ? GL.GL_BACK : GL.GL_FRONT);
			}
            if(requested.ColorWrite != applied.ColorWrite) {
                applied.ColorWrite = requested.ColorWrite;
                if(requested.ColorWrite) {
                    GL.ColorMask(true, true, true, true);
                } else {
                    GL.ColorMask(false, false, false, false);
                }
            }
			if (requested.DepthWrite != applied.DepthWrite)
			{
				GL.DepthMask(requested.DepthWrite);
				applied.DepthWrite = requested.DepthWrite;
			}
            ApplyScissor();
            if (requested.PolygonOffset != applied.PolygonOffset)
            {
                applied.PolygonOffset = requested.PolygonOffset;
                GL.PolygonOffset(requested.PolygonOffset.X, requested.PolygonOffset.Y);
            }
        }
	}
}

