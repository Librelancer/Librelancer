// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using LibreLancer.Graphics.Backends;
using LibreLancer.Graphics.Backends.OpenGL;

namespace LibreLancer.Graphics
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
		public Color4 ClearColor
        {
            get => requested.ClearColor;
            set => requested.ClearColor = value;
        }

        public bool SupportsWireframe => impl.SupportsWireframe;

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

        public int MaxAnisotropy => impl.MaxAnisotropy;
        public int MaxSamples => impl.MaxSamples;

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
		public ushort BlendMode
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

        private RenderTarget requestedRenderTarget;
        public RenderTarget RenderTarget
        {
            get => requestedRenderTarget;
            set
            {
                Renderer2D.Flush();
                requestedRenderTarget = value;
                requested.RenderTarget = requestedRenderTarget?.Target;
            }
        }

        private Shader requestedShader;

        public Shader Shader
        {
            get => requestedShader;
            set
            {
                Renderer2D.Flush();
                requestedShader = value;
                requested.Shader = value.Backing;
            }
        }

        public CullFaces CullFace
        {
            get => requested.CullFaces;
            set => requested.CullFaces = value;
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

        public Renderer2D Renderer2D { get; }

        private IRenderContext impl;

        internal IRenderContext Backend => impl;

        public bool HasFeature(GraphicsFeature feature) => impl.HasFeature(feature);

        internal RenderContext (IRenderContext impl)
        {
            this.impl = impl;
			Instance = this;
			PreferredFilterLevel = TextureFiltering.Trilinear;
            impl.Init(ref requested);
            Renderer2D = new Renderer2D(this);
            cameraBuffer = new UniformBuffer(this, 1, Marshal.SizeOf<CameraMatrices>(), typeof(CameraMatrices));
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

        public void SSBOMemoryBarrier() => impl.MemoryBarrier();

        public void ClearAll()
		{
			Apply();
			impl.ClearAll();
            frameNumber++;
        }

		public void ClearDepth()
		{
            Apply();
            impl.ClearDepth();
        }

        private GraphicsState requested;

        internal void ApplyViewport() => impl.ApplyViewport(ref requested);

        internal void EndFrame()
        {
            Renderer2D.Flush();
            if (viewports.Count != 1)
                throw new Exception ("viewports.Count != 1 at end of frame");
        }

        internal void ApplyScissor() => impl.ApplyScissor(ref requested);

        internal void ApplyRenderTarget() => impl.ApplyRenderTarget(ref requested);

        internal void SetBlendMode(ushort mode) => impl.SetBlendMode(mode);
        internal void Set2DState(bool depth, bool cull, bool scissor) => impl.Set2DState(depth, cull, scissor);

		public void Apply()
		{
            Renderer2D.Flush();
            ApplyRenderTarget();
            ApplyViewport();
            impl.ApplyState(ref requested);
        }
	}
}

