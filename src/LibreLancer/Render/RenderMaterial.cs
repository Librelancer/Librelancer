// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Vertices;
using LibreLancer.Utf.Mat;
using LibreLancer.Utf.Cmp;
namespace LibreLancer
{

	public abstract class RenderMaterial
	{
        public static bool VertexLighting = false;
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
        public virtual bool DisableCull {  get { return false; } }
        public bool DoubleSided = false;
		Texture2D[] textures = new Texture2D[8];
		bool[] loaded = new bool[8];
		protected static bool HasSpotlight(ref Lighting lights)
		{
            if (lights.Lights.SourceLighting == null) return false;
			for (int i = 0; i < lights.Lights.SourceLighting.Lights.Count; i++)
			{
				if (lights.Lights.SourceLighting.Lights[i].Light.Kind == LightKind.Spotlight) return true;
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

        static void SetLight(ShaderVariables shader, bool hasSpotlight, int i, ref RenderLight lt)
        {
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
		public static void SetLights(ShaderVariables shader, ref Lighting lights)
		{
            if (!lights.Enabled) {
                shader.SetLightParameters(new Vector4i(0, 0, 0, -1));
                return;
            }
            shader.SetAmbientColor(new Color4(lights.Ambient,1));
			bool hasSpotlight = HasSpotlight(ref lights);
            int count = 0;
            if(lights.Lights.SourceLighting != null) {
                for (int i = 0; i < lights.Lights.SourceLighting.Lights.Count; i++)
                {
                    if (lights.Lights.SourceEnabled[i])
                        SetLight(shader, hasSpotlight, count++, ref lights.Lights.SourceLighting.Lights[i].Light);
                }
            }
            if (lights.Lights.NebulaCount == 1)
                SetLight(shader, hasSpotlight, count++, ref lights.Lights.Nebula0);
            shader.SetLightParameters(new Vector4i(1, count, (int)lights.FogMode, lights.NumberOfTilesX));
			if (lights.FogMode == FogModes.Linear)
			{
				shader.SetFogColor(new Color4(lights.FogColor,1));
				shader.SetFogRange(lights.FogRange);
			}
			else if (lights.FogMode == FogModes.Exp || lights.FogMode == FogModes.Exp2)
			{
				shader.SetFogColor(new Color4(lights.FogColor,1));
				shader.SetFogRange(new Vector2(lights.FogRange.X, 0));
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
            if (tex2d == null)
                tex2d = (Texture2D)Library.FindTexture(ResourceManager.NullTextureName);
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

