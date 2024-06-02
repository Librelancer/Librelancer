// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Shaders;
using LibreLancer.Utf.Dfm;
using LibreLancer.Utf.Mat;
using LibreLancer.Utf.Vms;

namespace LibreLancer.Render.Materials
{
	public class BasicMaterial : RenderMaterial
    {
        public const int ForceAlpha = (1 << 31);

		public string Type;

		public Color4 Dc = Color4.White;
		public string DtSampler;
		public SamplerFlags DtFlags;
		public float Oc = 1f;
		public bool OcEnabled = false;
		public bool EtEnabled = false;
		public bool AlphaEnabled = false;
        public bool AlphaTest = false;
		public Color4 Ec = Color4.White;
		public string EtSampler;
		public SamplerFlags EtFlags;
        public string NtSampler;
        public SamplerFlags NtFlags;
        public string MtSampler;
        public SamplerFlags MtFlags;
        public string RtSampler;
        public SamplerFlags RtFlags;

		public BasicMaterial(string type, ResourceManager library) : base(library)
		{
			Type = type;
		}

		static ShaderVariables GetShader(RenderContext rstate, IVertexType vertextype, ShaderFeatures caps, bool normalMap, bool pbr)
        {
            if (vertextype is Utf.Dfm.DfmVertex)
                return Basic_Skinned.Get(rstate, caps);
            else if (vertextype is FVFVertex fvf)
            {
                if (pbr)
                {
                    var x = caps | (normalMap ? ShaderFeatures.NORMALMAP : 0);
                    x |= ShaderFeatures.METALROUGHMAP;
                    return PBR.Get(rstate, x);
                }
                if (fvf.Diffuse && fvf.Normal) {
                    return Basic_PositionNormalColorTexture.Get(rstate, caps);
                }
                else if (fvf.Normal)
                {
                    if (fvf.TexCoords == 2)
                    {
                        return Basic_PositionNormalTextureTwo.Get(rstate, caps);
                    }
                    else if (fvf.TexCoords == 4)
                    {
                        var x = caps | (normalMap ? ShaderFeatures.NORMALMAP : 0);
                        return Basic_PositionNormalTextureTwo.Get(rstate, x);
                    }
                    else if (fvf.TexCoords == 1)
                    {
                        return Basic_PositionNormalTexture.Get(rstate, caps);
                    }
                    else if (fvf.TexCoords == 3)
                    {
                        var x = caps | (normalMap ? ShaderFeatures.NORMALMAP : 0);
                        return Basic_PositionNormalTexture.Get(rstate, x);
                    }
                }
                if (fvf.Diffuse)
                    return Basic_PositionColor.Get(rstate, caps);
                return Basic_PositionTexture.Get(rstate, caps);
            }
            if (vertextype is VertexPositionNormalTexture ||
                    vertextype is VertexPositionNormal)
                return Basic_PositionNormalTexture.Get(rstate, caps);
            if (vertextype is VertexPositionNormalDiffuseTexture)
                return Basic_PositionNormalColorTexture.Get(rstate, caps);
            if (vertextype is VertexPositionTexture)
                return Basic_PositionTexture.Get(rstate, caps);
            if (vertextype is VertexPosition)
                return Basic_PositionTexture.Get(rstate, caps);

            return Basic_PositionColor.Get(rstate, caps);
		}

        public override void Use(RenderContext rstate, IVertexType vertextype, ref Lighting lights, int userData)
		{
            ShaderFeatures caps = ShaderFeatures.None;
            bool normalMap = !string.IsNullOrWhiteSpace(NtSampler);
            bool pbr = (!string.IsNullOrWhiteSpace(RtSampler) ||
                       !string.IsNullOrWhiteSpace(MtSampler)) &&
                       lights.Enabled;
            if (VertexLighting)
            {
                caps |= ShaderFeatures.VERTEX_LIGHTING;
            }
            if (EtEnabled) caps |= ShaderFeatures.ET_ENABLED;
            if (Fade) caps |= ShaderFeatures.FADE_ENABLED;
            var dxt1 = GetDxt1();
            if (dxt1 || AlphaTest)
            {
                caps |= ShaderFeatures.ALPHATEST_ENABLED;
                //Shitty way of dealing with alpha_mask
                //FL has a lot of DXT1 textures that aren't part of alpha_mask
                //so this brings overall performance down.
                //Don't change any of this stuff unless you can verify it works
                //in all places! (Check Li01 shipyards, Bw10 tradelanes)
            }
			var shader = GetShader(rstate, vertextype, caps, normalMap, pbr);
			shader.SetWorld(World);
            //Dt
			shader.SetDtSampler(0);
			BindTexture(rstate, 0, DtSampler, 0, DtFlags, ResourceManager.WhiteTextureName);
			//Dc
			shader.SetDc(Dc);
			//Oc
			shader.SetOc(Oc);
			if (AlphaEnabled || Fade || OcEnabled || dxt1 || AlphaTest || (userData & ForceAlpha) == ForceAlpha)
			{
				rstate.BlendMode = BlendMode.Normal;
			}
			else
			{
                rstate.BlendMode = BlendMode.Opaque; //TODO: Maybe I can just leave this out?
			}
			//Fade
			if (Fade) shader.SetFadeRange(new Vector2(FadeNear, FadeFar));
			//MaterialAnim
			if (MaterialAnim != null)
			{
				shader.SetMaterialAnim(new Vector4(
					MaterialAnim.UOffset,
					MaterialAnim.VOffset,
					MaterialAnim.UScale,
					MaterialAnim.VScale
				));
			}
			else
			{
				shader.SetMaterialAnim(new Vector4(0, 0, 1, 1));
			}
            if (Bones != null && vertextype is DfmVertex) {
                shader.Shader.UniformBlockBinding("Bones", 1);
                shader.SetSkinningEnabled(true);
                Bones.BindTo(1, BufferOffset, 200);
            }
            else
                shader.SetSkinningEnabled(false);
            //Ec
			shader.SetEc(Ec);
			//EtSampler
			if (EtEnabled)
			{
				shader.SetEtSampler(1);
				BindTexture(rstate, 1, EtSampler, 1, EtFlags, ResourceManager.NullTextureName);
			}
            if (normalMap)
            {
                shader.SetNtSampler(2);
                BindTexture(rstate, 2, NtSampler, 2, NtFlags, ResourceManager.NullTextureName);
            }

            if (pbr)
            {
                shader.SetMtSampler(3);
                BindTexture(rstate, 3, MtSampler, 3, MtFlags, ResourceManager.WhiteTextureName);
                shader.SetMetallic(1.0f);
                shader.SetRtSampler(5);
                BindTexture(rstate, 5, RtSampler, 5, RtFlags, ResourceManager.WhiteTextureName);
                shader.SetRoughness(1.0f);
            }
			//Set lights
            SetLights(shader, ref lights, rstate.FrameNumber);
            rstate.Shader = shader;
        }

		public override void ApplyDepthPrepass(RenderContext rstate)
		{
			rstate.BlendMode = BlendMode.Normal;
            //TODO: This is screwy - Re-do DXT1 test if need be for perf
			var shader = DepthPass_AlphaTest.Get(rstate);
            BindTexture(rstate, 0, DtSampler, 0, DtFlags, ResourceManager.WhiteTextureName);
			shader.SetWorld(World);
            rstate.Shader = shader;
        }

        bool GetDxt1()
        {
            var tex = GetTexture(0, DtSampler);
            if (tex != null) return tex.Dxt1;
            return false;
        }
		public override bool IsTransparent
		{
			get
			{
                return AlphaEnabled && !GetDxt1();
			}
		}
	}
}

