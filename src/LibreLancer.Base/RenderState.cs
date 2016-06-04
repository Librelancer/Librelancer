/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
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
				/*if (blendEnabled && blend == BlendMode.Opaque)
					GL.Disable (EnableCap.Blend);
				if (!blendEnabled && blend != BlendMode.Opaque)
					GL.Enable (EnableCap.Blend);
				blendEnabled = blend != BlendMode.Opaque;
				if(blend == BlendMode.Additive)
					GL.BlendFunc (BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One);
				if(blend == BlendMode.Normal)
					GL.BlendFunc (BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);*/
				switch (blend)
				{
					case BlendMode.Normal:
						GL.Enable(EnableCap.Blend);
						GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
						break;
					case BlendMode.Opaque:
						GL.Disable(EnableCap.Blend);
						break;
					case BlendMode.Additive:
						GL.Enable(EnableCap.Blend);
						GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One);
						break;
				}
				blendDirty = false;
			}
		}
	}
}

