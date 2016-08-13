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
using LibreLancer.Vertices;
using LibreLancer.Utf.Mat;

namespace LibreLancer
{
	public abstract class RenderMaterial
	{
		public Matrix4 World = Matrix4.Identity;
		public Matrix4 View = Matrix4.Identity;
		public Matrix4 Projection = Matrix4.Identity;
		public Matrix4 ViewProjection = Matrix4.Identity;
		public ILibFile Library;
		public abstract void Use (RenderState rstate, IVertexType vertextype, Lighting lights);
		static Texture2D nullTexture;
		public abstract bool IsTransparent { get; }
		Texture2D[] textures = new Texture2D[8];
		public static void SetLights(Shader shader, Lighting lights)
		{
			var h = lights.Hash;
			if (shader.UserTag == h)
				return;
			shader.UserTag = h;
			shader.SetInteger ("LightingEnabled", lights.Enabled ? 1 : 0);
			if (!lights.Enabled)
				return;
			shader.SetColor4 ("AmbientColor", lights.Ambient);
			shader.SetInteger ("LightCount", lights.Lights.Count);
			for (int i = 0; i < lights.Lights.Count; i++) {
				var lt = lights.Lights [i];
				shader.SetVector4 ("LightsPos", new Vector4(lt.Position, lt.Kind != LightKind.Directional ? 1 : 0), i);
				shader.SetVector3 ("LightsDir", lt.Direction, i);
				shader.SetVector3 ("LightsColor", new Vector3(lt.Color.R, lt.Color.G, lt.Color.B), i);
				shader.SetVector4 ("LightsAttenuation", lt.Attenuation, i);
				shader.SetInteger ("LightsRange", lt.Range, i);
			}
			shader.SetInteger("FogEnabled", lights.FogEnabled ? 1 : 0);
			if (lights.FogEnabled)
			{
				shader.SetColor4("FogColor", lights.FogColor);
				shader.SetVector2("FogRange", lights.FogRange);
			}
		}
		protected void BindTexture(int cacheidx, string tex, int unit, SamplerFlags flags, bool throwonNull = true)
		{
			if (tex == null)
			{
				if (throwonNull)
					throw new Exception();
				if (nullTexture == null)
				{
					nullTexture = new Texture2D(256, 256, false, SurfaceFormat.Color);
					Color4b[] colors = new Color4b[nullTexture.Width * nullTexture.Height];
					for (int i = 0; i < colors.Length; i++)
						colors[i] = Color4b.White;
					nullTexture.SetData<Color4b>(colors);
				}
				nullTexture.BindTo(unit);
			}
			else
			{
				if(textures[cacheidx] == null)
					textures[cacheidx] = (Texture2D)Library.FindTexture(tex);
				var tex2d = textures[cacheidx];
				if (tex2d.IsDisposed)
					tex2d = textures[cacheidx] = (Texture2D)Library.FindTexture(tex);
				tex2d.BindTo(unit);
				if ((flags & SamplerFlags.ClampToEdgeU) == SamplerFlags.ClampToEdgeU) {
					tex2d.SetWrapModeS (WrapMode.ClampToEdge);
				}
				if ((flags & SamplerFlags.ClampToEdgeV) == SamplerFlags.ClampToEdgeV) {
					tex2d.SetWrapModeT (WrapMode.ClampToEdge);
				}
				if ((flags & SamplerFlags.MirrorRepeatU) == SamplerFlags.MirrorRepeatU) {
					tex2d.SetWrapModeS (WrapMode.MirroredRepeat);
				}
				if ((flags & SamplerFlags.MirrorRepeatU) == SamplerFlags.MirrorRepeatV) {
					tex2d.SetWrapModeT(WrapMode.MirroredRepeat);
				}
			}

		}
	}
}

