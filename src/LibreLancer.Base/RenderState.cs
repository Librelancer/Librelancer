// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;

namespace LibreLancer
{
	public enum DepthFunction
	{
		Less = 0x201,
		LessEqual = 0x203
	}
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
			get
			{
				return depthwrite;
			} set
			{
				if (depthwrite == value)
					return;
				depthwrite = value;
				depthwritedirty = true;
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

		public DepthFunction DepthFunction
		{
			set
			{
				GL.DepthFunc((int)value);
			}
		}

        bool colorWrite = true;
        bool requestedColorWrite = true;
        public bool ColorWrite
        {
            get {
                return requestedColorWrite;
            } set {
                requestedColorWrite = value;
            }
        }
		public bool Cull {
			get {
				return cull;
			} set {
				if (cull == value)
					return;
				cull = value;
				cullDirty = true;
			}
		}
        public bool ScissorEnabled { get => scissorEnabled; set => scissorEnabled = value; }
        public Rectangle ScissorRectangle { get => scissorRect; set => scissorRect = value; }

        CullFaces requestedCull = CullFaces.Back;
		CullFaces cullFace = CullFaces.Back;
		public CullFaces CullFace
		{
			get {
				return requestedCull;
			} set {
				requestedCull = value;
			}
		}

		internal void Trash()
		{
			cullDirty = true;
			clearDirty = true;
			wireframeDirty = true;
			depthDirty = true;
			blendDirty = true;
		}

		bool cull = true;
		bool cullDirty = false;

		Color4 clearColor = Color4.Black;
		bool clearDirty = false;

		bool isWireframe = false;
		bool wireframeDirty = false;

		bool depthEnabled = true;
		bool depthDirty = false;

		BlendMode blend = BlendMode.Normal;
		bool blendDirty = false;
		bool blendEnable = true;

		bool depthwrite = true;
		bool depthwritedirty = false;
        public uint NullVAO;

        bool scissorEnabled = false;
        bool currentScissorEnabled = false;
        Rectangle scissorRect;
        Rectangle currentScissorRect;
        bool scissorVpChanged;
        int vpHeight = 0;

		public RenderState ()
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
            if(GLExtensions.Anisotropy) {
                int af;
                GL.GetIntegerv(GL.GL_MAX_TEXTURE_MAX_ANISOTROPY_EXT, out af);
                MaxAnisotropy = af;
                FLLog.Debug("GL", "Max Anisotropy: " + af);
            } else {
                MaxAnisotropy = 0;
                FLLog.Debug("GL", "Anisotropic Filter Not Supported!");
            }
		}

		public void SetViewport(int x, int y, int w, int h)
		{
			GL.Viewport(x,y,w,h);
            scissorVpChanged = true;
            vpHeight = h;
		}

		public void ClearAll()
		{
			Apply();
			GL.Clear (GL.GL_COLOR_BUFFER_BIT | GL.GL_DEPTH_BUFFER_BIT);
		}

		public void ClearDepth()
		{
            Apply();
            GL.Clear (GL.GL_DEPTH_BUFFER_BIT);
		}
		public void Apply()
		{
			if (clearDirty) {
				GL.ClearColor (clearColor.R, clearColor.G, clearColor.B, clearColor.A);
				clearDirty = false;
			}

			if (wireframeDirty) {
				GL.PolygonMode (GL.GL_FRONT_AND_BACK, isWireframe ? GL.GL_LINE : GL.GL_FILL);
				if (isWireframe)
					GL.LineWidth(1.7f);
				else
					GL.LineWidth(1);
				wireframeDirty = false;
			}

			if (depthDirty) {
				if (depthEnabled)
					GL.Enable (GL.GL_DEPTH_TEST);
				else
					GL.Disable (GL.GL_DEPTH_TEST);
				depthDirty = false;
			}

			if (blendDirty) {
				if (!blendEnable && blend != BlendMode.Opaque) {
					GL.Enable (GL.GL_BLEND);
					blendEnable = true;
				}
				if (blendEnable && blend == BlendMode.Opaque) {
					GL.Disable (GL.GL_BLEND);
					blendEnable = false;
				}
				switch (blend)
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
				}
				blendDirty = false;
			}
			if (cullDirty) {
				if (cull)
					GL.Enable (GL.GL_CULL_FACE);
				else
					GL.Disable (GL.GL_CULL_FACE);
				cullDirty = false;
			}
			if (requestedCull != cullFace)
			{
				cullFace = requestedCull;
				GL.CullFace(cullFace == CullFaces.Back ? GL.GL_BACK : GL.GL_FRONT);
			}
            if(colorWrite != requestedColorWrite) {
                colorWrite = requestedColorWrite;
                if(colorWrite) {
                    GL.ColorMask(true, true, true, true);
                } else {
                    GL.ColorMask(false, false, false, false);
                }
            }
			if (depthwritedirty)
			{
				GL.DepthMask(depthwrite);
				depthwritedirty = false;
			}
            if(scissorEnabled != currentScissorEnabled)
            {
                if (scissorEnabled) GL.Enable(GL.GL_SCISSOR_TEST);
                else GL.Disable(GL.GL_SCISSOR_TEST);
                currentScissorEnabled = scissorEnabled;
            }
            if(scissorEnabled & (scissorVpChanged || currentScissorRect != scissorRect))
            {
                currentScissorRect = scissorRect;
                scissorVpChanged = false;
                GL.Scissor(scissorRect.X, vpHeight - scissorRect.Y - scissorRect.Height, scissorRect.Width, scissorRect.Height);
            }

        }
	}
}

