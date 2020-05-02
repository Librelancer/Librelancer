// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Vertices;
using LibreLancer.Utf.Mat;
using LibreLancer.Utf.Cmp;

namespace LibreLancer
{
	public abstract class RenderMaterial
	{
        public static bool VertexLighting = false;
		public MaterialAnim MaterialAnim;
		public WorldMatrixHandle World = new WorldMatrixHandle();
		public bool FlipNormals = false;
		public ICamera Camera;
		public ILibFile Library;
		public bool Fade = false;
		public float FadeNear = 0;
		public float FadeFar = 0;
        public UniformBuffer Bones;
        public int BufferOffset;
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

        private const int MAX_SET_LIGHTS = 10;

        struct PackedLight
        {
            public Vector4 Position;
            public Vector4 ColorRange;
            public Vector4 Attenuation;
        }
        struct PackedSpotlight
        {
            public Vector4 Direction;
            public Vector4 SpotlightParams;
        }
		public static unsafe void SetLights(ShaderVariables shader, ref Lighting lighting)
		{
            if (!lighting.Enabled) {
                shader.SetLightParameters(new Vector4i(0, 0, 0, -1));
                return;
            }
            shader.SetAmbientColor(new Color4(lighting.Ambient,1));
			bool hasSpotlight = HasSpotlight(ref lighting);
            //Prepare shader array (TODO: make faster?)
            PackedSpotlight* spots = stackalloc PackedSpotlight[MAX_SET_LIGHTS];
            PackedLight* pLights = stackalloc PackedLight[MAX_SET_LIGHTS];
            int lightCount = 0;
            if(lighting.Lights.SourceLighting != null) {
                for (int i = 0; i < lighting.Lights.SourceLighting.Lights.Count; i++)
                {
                    if (lighting.Lights.SourceEnabled[i])
                    {
                        var lt = lighting.Lights.SourceLighting.Lights[i].Light;
                        float kind = 0;
                        if (lt.Kind == LightKind.Point || lt.Kind == LightKind.Spotlight)
                            kind = 1;
                        else if (lt.Kind == LightKind.PointAttenCurve)
                            kind = 2;
                        if (lightCount + 1 >= MAX_SET_LIGHTS) throw new Exception("Internal too many lights");
                        pLights[lightCount].Position = new Vector4(lt.Kind == LightKind.Directional ? lt.Direction : lt.Position, kind);
                        pLights[lightCount].ColorRange = new Vector4(lt.Color.R, lt.Color.G, lt.Color.B, lt.Range);
                        pLights[lightCount].Attenuation = new Vector4(lt.Attenuation, 0);
                        if (hasSpotlight && lt.Kind == LightKind.Spotlight)
                        {
                            spots[lightCount].Direction = new Vector4(lt.Direction, 1);
                            spots[lightCount].SpotlightParams = new Vector4(lt.Falloff, (float) (Math.Cos(lt.Theta / 2.0)), (float) (Math.Cos(lt.Phi / 2.0)), 1);
                        }

                        lightCount++;
                    }
                }
            }
            if (lighting.Lights.NebulaCount == 1)
            {
                if (lightCount + 1 >= MAX_SET_LIGHTS) throw new Exception("Internal too many lights");
                var lt = lighting.Lights.Nebula0;
                float kind = 0;
                if (lt.Kind == LightKind.Point || lt.Kind == LightKind.Spotlight)
                    kind = 1;
                else if (lt.Kind == LightKind.PointAttenCurve)
                    kind = 2;
                if (lightCount + 1 >= MAX_SET_LIGHTS) throw new Exception("Internal too many lights");
                pLights[lightCount].Position = new Vector4(lt.Kind == LightKind.Directional ? lt.Direction : lt.Position, kind);
                pLights[lightCount].ColorRange = new Vector4(lt.Color.R, lt.Color.G, lt.Color.B, lt.Range);
                pLights[lightCount].Attenuation = new Vector4(lt.Attenuation, 0);
                if (hasSpotlight && lt.Kind == LightKind.Spotlight)
                {
                    spots[lightCount].Direction = new Vector4(lt.Direction, 1);
                    spots[lightCount].SpotlightParams = new Vector4(lt.Falloff, (float) (Math.Cos(lt.Theta / 2.0)), (float) (Math.Cos(lt.Phi / 2.0)), 1);
                }
                lightCount++;
            }
            //Upload!
            shader.SetLightData((Vector4*)pLights, lightCount * 3);
            if(hasSpotlight)
                shader.SetSpotlightData((Vector4*)spots, lightCount * 2);
            shader.SetLightParameters(new Vector4i(1, lightCount, (int)lighting.FogMode, lighting.NumberOfTilesX));
            if (lighting.FogMode == FogModes.Linear)
			{
				shader.SetFogColor(new Color4(lighting.FogColor,1));
				shader.SetFogRange(lighting.FogRange);
			}
			else if (lighting.FogMode == FogModes.Exp || lighting.FogMode == FogModes.Exp2)
			{
				shader.SetFogColor(new Color4(lighting.FogColor,1));
				shader.SetFogRange(new Vector2(lighting.FogRange.X, 0));
			}
		}

		protected Texture2D GetTexture(int cacheidx, string tex)
		{
			if (tex == null)
				return (Texture2D)Library.FindTexture(ResourceManager.NullTextureName);
			if (textures[cacheidx] == null)
				textures[cacheidx] = (Texture2D)Library.FindTexture(tex);
			var tex2d = textures[cacheidx];
            if (tex2d == null) return tex2d;
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

