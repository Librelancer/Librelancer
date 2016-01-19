using System;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
namespace LibreLancer
{
	//OpenGL Render States
	public class RenderState
	{
		static internal RenderState Instance;
		public Color4 ClearColor {
			get {
				return clearColor;
			} set {
				if (value == clearColor)
					return;
				clearColor = value;
				clearDirty = true;
			}
		}

		public bool Wireframe {
			get {
				return isWireframe;
			} set {
				if (value == isWireframe)
					return;
				isWireframe = value;
				wireframeDirty = true;
			}
		}

		public bool DepthEnabled {
			get {
				return depthEnabled;
			} set {
				if (depthEnabled == value)
					return;
				depthEnabled = value;
				depthDirty = true;
			}
		}

		public BlendMode BlendMode {
			get {
				return blend;
			} set {
				if (blend == value)
					return;
				blendDirty = true;
				blend = value;
			}
		}

		Color4 clearColor = Color4.Black;
		bool clearDirty = false;

		bool isWireframe = false;
		bool wireframeDirty = false;

		bool depthEnabled = true;
		bool depthDirty = false;

		BlendMode blend = BlendMode.Normal;
		bool blendDirty = false;
		bool blendEnabled = true;

		public RenderState ()
		{
			GL.ClearColor (Color4.Black);
			GL.Enable (EnableCap.Blend);
			GL.Enable (EnableCap.DepthTest);
			GL.BlendFunc (BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			GL.DepthFunc (DepthFunction.Lequal);
			GL.Enable (EnableCap.CullFace);
			GL.CullFace (CullFaceMode.Back);
			Instance = this;
		}

		public void ClearAll()
		{
			GL.Clear (ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
		}

		public void ClearDepth()
		{
			GL.Clear (ClearBufferMask.DepthBufferBit);
		}

		internal void Apply()
		{
			if (clearDirty) {
				GL.ClearColor (clearColor);
				clearDirty = false;
			}

			if (wireframeDirty) {
				GL.PolygonMode (MaterialFace.FrontAndBack, isWireframe ? PolygonMode.Line : PolygonMode.Fill);
				wireframeDirty = false;
			}

			if (depthDirty) {
				if (depthEnabled)
					GL.Enable (EnableCap.DepthTest);
				else
					GL.Disable (EnableCap.DepthTest);
				depthDirty = false;
			}

			if (blendDirty) {
				if (blendEnabled && blend == BlendMode.Opaque)
					GL.Disable (EnableCap.Blend);
				if (!blendEnabled && blend != BlendMode.Opaque)
					GL.Enable (EnableCap.Blend);
				blendEnabled = blend != BlendMode.Opaque;
				if(blend == BlendMode.Additive)
					GL.BlendFunc (BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One);
				if(blend == BlendMode.Normal)
					GL.BlendFunc (BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
				blendDirty = false;
			}
		}
	}
}

