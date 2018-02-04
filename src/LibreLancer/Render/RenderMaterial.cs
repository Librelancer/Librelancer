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
using LibreLancer.Utf.Cmp;
namespace LibreLancer
{

	public abstract class RenderMaterial
	{
		public MaterialAnim MaterialAnim;
		public Matrix4 World = Matrix4.Identity;
		public bool FlipNormals = false;
		public ICamera Camera;
		public ILibFile Library;
		public bool Fade = false;
		public float FadeNear = 0;
		public float FadeFar = 0;
		public abstract void Use(RenderState rstate, IVertexType vertextype, ref Lighting lights);
        public virtual void UpdateFlipNormals() {} //Optimisation
		public abstract bool IsTransparent { get; }
		public bool DoubleSided = false;
		Texture2D[] textures = new Texture2D[8];
		bool[] loaded = new bool[8];
		protected static bool HasSpotlight(ref Lighting lights)
		{
			for (int i = 0; i < lights.Lights.Count; i++)
			{
				if (lights.Lights[i].Kind == LightKind.Spotlight) return true;
			}
			return false;
		}

		static ShaderVariables _normalPrepass;
		protected static ShaderVariables NormalPrepassShader
		{
			get
			{
				if (_normalPrepass == null) _normalPrepass = ShaderCache.Get("DepthPrepass_Normal.vs", "DepthPrepass_Normal.frag");
				return _normalPrepass;
			}
		}

		static ShaderVariables _alphaPrepass;
		protected static ShaderVariables AlphaTestPrepassShader
		{
			get
			{
				if (_alphaPrepass == null) _alphaPrepass = ShaderCache.Get("DepthPrepass_AlphaTest.vs", "DepthPrepass_AlphaTest.frag");
				return _alphaPrepass;
			}
		}

		public abstract void ApplyDepthPrepass(RenderState rstate);

		public static void SetLights(ShaderVariables shader, ref Lighting lights)
		{
			bool hasSpotlight = HasSpotlight(ref lights);
			var h = lights.Hash;
			if (shader.UserTag == h)
				return;
			shader.UserTag = h;
			shader.SetLightingEnabled(lights.Enabled ? 1 : 0);
			if (!lights.Enabled)
				return;
			shader.SetAmbientColor(lights.Ambient);
			shader.SetLightCount(lights.Lights.Count);
			shader.SetTilesX(lights.NumberOfTilesX);
			for (int i = 0; i < lights.Lights.Count; i++)
			{
				var lt = lights.Lights[i];
				float kind = 0;
				if (lt.Kind == LightKind.Point || lt.Kind == LightKind.Spotlight)
					kind = 1;
				else if (lt.Kind == LightKind.PointAttenCurve)
					kind = 2;
				shader.SetLightsPos(i, new Vector4(lt.Kind == LightKind.Directional ? lt.Direction : lt.Position, kind));
				shader.SetLightsColorRange(i, new Vector4(lt.Color.R, lt.Color.G, lt.Color.B, lt.Range));
				shader.SetLightsAttenuation(i, lt.Attenuation);
				if (hasSpotlight)
				{
					if (lt.Kind == LightKind.Spotlight)
					{
						shader.SetLightsDir(i, lt.Direction);
						shader.SetSpotlightParams(i, new Vector3(lt.Falloff, (float)(Math.Cos(lt.Theta / 2.0)), (float)(Math.Cos(lt.Phi / 2.0))));
					}
					else if (lt.Kind == LightKind.Point || lt.Kind == LightKind.PointAttenCurve)
					{
						shader.SetSpotlightParams(i, Vector3.Zero);
					}
				}
			}
			shader.SetFogMode((int)lights.FogMode);
			if (lights.FogMode == FogModes.Linear)
			{
				shader.SetFogColor(lights.FogColor);
				shader.SetFogRange(lights.FogRange);
			}
			else if (lights.FogMode == FogModes.Exp || lights.FogMode == FogModes.Exp2)
			{
				shader.SetFogColor(lights.FogColor);
				shader.SetFogRange(new Vector2(lights.FogDensity, 0));
			}
		}

		protected Texture2D GetTexture(int cacheidx, string tex)
		{
			if (tex == null)
				return (Texture2D)Library.FindTexture(ResourceManager.NullTextureName);
			if (textures[cacheidx] == null)
				textures[cacheidx] = (Texture2D)Library.FindTexture(tex);
			var tex2d = textures[cacheidx];
			if (tex2d.IsDisposed)
				tex2d = textures[cacheidx] = (Texture2D)Library.FindTexture(tex);
			return textures[cacheidx];
		}

		protected void BindTexture(RenderState rstate, int cacheidx, string tex, int unit, SamplerFlags flags, string nullName = null)
		{
			if (tex == null)
			{
				if (nullName == null)
					throw new Exception();
				tex = nullName;
			}
			if (textures[cacheidx] == null || !loaded[cacheidx])
				textures[cacheidx] = (Texture2D)Library.FindTexture(tex);
			if (textures[cacheidx] == null)
			{
				textures[cacheidx] = (Texture2D)Library.FindTexture(ResourceManager.NullTextureName);
				loaded[cacheidx] = false;
			}
			else
				loaded[cacheidx] = true;
			var tex2d = textures[cacheidx];
			if (tex2d.IsDisposed)
				tex2d = textures[cacheidx] = (Texture2D)Library.FindTexture(tex);
			tex2d.BindTo(unit);
			tex2d.SetFiltering(rstate.PreferredFilterLevel);
			if ((flags & SamplerFlags.ClampToEdgeU) == SamplerFlags.ClampToEdgeU)
			{
				tex2d.SetWrapModeS(WrapMode.ClampToEdge);
			}
			if ((flags & SamplerFlags.ClampToEdgeV) == SamplerFlags.ClampToEdgeV)
			{
				tex2d.SetWrapModeT(WrapMode.ClampToEdge);
			}
			if ((flags & SamplerFlags.MirrorRepeatU) == SamplerFlags.MirrorRepeatU)
			{
				tex2d.SetWrapModeS(WrapMode.MirroredRepeat);
			}
			if ((flags & SamplerFlags.MirrorRepeatU) == SamplerFlags.MirrorRepeatV)
			{
				tex2d.SetWrapModeT(WrapMode.MirroredRepeat);
			}

		}
	}
}

