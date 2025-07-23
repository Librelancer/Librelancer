// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Runtime.InteropServices;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Resources;
using LibreLancer.Shaders;
using LibreLancer.Utf.Dfm;
using LibreLancer.Utf.Mat;
using LibreLancer.Utf.Vms;

namespace LibreLancer.Render.Materials
{
	public class BasicMaterial : RenderMaterial
    {
        public const int ForceAlpha = (1 << 31);
        public const int DcSet = (1 << 24);

        public static int SetDc(Color4 color)
        {
            var x = (VertexDiffuse)color;
            x.A = 1;
            return (int)x.Pixel;
        }

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
        public string NmSampler;
        public SamplerFlags NmFlags;
        public string MtSampler;
        public SamplerFlags MtFlags;
        public string RtSampler;
        public SamplerFlags RtFlags;
        public float? Metallic;
        public float? Roughness;

		public BasicMaterial(string type, ResourceManager library) : base(library)
		{
			Type = type;
		}


        [Flags]
        enum PBRFeatures : uint
        {
            NORMALMAP = (1 << 0),
            METALMAP = (1 << 1),
            ROUGHMAP = (1 << 2),
            ET_ENABLED = (1 << 3),
            VERTEX_TEXTURE2 = (1 << 4)
        }

        Shader GetPBRShader(IVertexType vertexType)
        {
            var caps = (PBRFeatures)0;
            if (!string.IsNullOrEmpty(RtSampler))
                caps |= PBRFeatures.ROUGHMAP;
            if (!string.IsNullOrEmpty(NmSampler))
                caps |= PBRFeatures.NORMALMAP;
            if(!string.IsNullOrEmpty(MtSampler))
                caps |= PBRFeatures.METALMAP;
            if(EtEnabled)
                caps |= PBRFeatures.ET_ENABLED;
            if (vertexType is FVFVertex fvf)
            {
                if (fvf.TexCoords == 2 ||
                    fvf.TexCoords == 4)
                {
                    caps |= PBRFeatures.VERTEX_TEXTURE2;
                }
            }
            return AllShaders.PBR.Get(caps);
        }

        [Flags]
        enum ShaderFeatures : uint
        {
            VERTEX_LIGHTING = (1 << 0),
            ALPHATEST_ENABLED = (1 << 1),
            ET_ENABLED = (1 << 2),
            FADE_ENABLED = (1 << 3),
            NORMALMAP = (1 << 4),
            VERTEX_DIFFUSE = (1 << 5),
            VERTEX_TEXTURE2 = (1 << 6)
        }
        Shader GetRegularShader(IVertexType vertexType)
        {
            var caps = (ShaderFeatures)0;
            if (VertexLighting)
                caps |= ShaderFeatures.VERTEX_LIGHTING;
            if(EtEnabled)
                caps |= ShaderFeatures.ET_ENABLED;
            if (Fade)
                caps |= ShaderFeatures.FADE_ENABLED;
            //Shitty way of dealing with alpha_mask
            //FL has a lot of DXT1 textures that aren't part of alpha_mask
            //so this brings overall performance down.
            //Don't change any of this stuff unless you can verify it works
            //in all places! (Check Li01 shipyards, Bw10 tradelanes)
            if (AlphaTest || GetDxt1())
                caps |= ShaderFeatures.ALPHATEST_ENABLED;
            if (vertexType is Utf.Dfm.DfmVertex)
            {
                if (Bones != null)
                {
                    return AllShaders.Basic_Skinned.Get(caps);
                }
                return AllShaders.Basic_FVF.Get(caps);
            }
            if (vertexType is FVFVertex fvf)
            {
                if (fvf.Normal)
                {
                    bool normalMap = !string.IsNullOrEmpty(NmSampler);
                    if(fvf.Diffuse)
                    {
                        caps |= ShaderFeatures.VERTEX_DIFFUSE; // DIFFUSE
                    }
                    if (fvf.TexCoords is 2 or 4)
                    {
                        caps |= ShaderFeatures.VERTEX_TEXTURE2; // TEXTURE2
                    }
                    if (normalMap && (fvf.TexCoords is 3 or 4))
                    {
                        caps |= ShaderFeatures.NORMALMAP;
                    }
                    return AllShaders.Basic_FVF.Get(caps);
                }
                if (fvf.Diffuse)
                    return AllShaders.Basic_PositionColor.Get(caps);
                return AllShaders.Basic_PositionTexture.Get(caps);
            }
            if (vertexType is VertexPositionNormalTexture ||
                    vertexType is VertexPositionNormal)
                return AllShaders.Basic_FVF.Get( caps);
            if (vertexType is VertexPositionNormalDiffuseTexture)
                return AllShaders.Basic_FVF.Get( caps | (ShaderFeatures)(1 << 5));
            if (vertexType is VertexPositionTexture)
                return AllShaders.Basic_PositionTexture.Get(caps);
            if (vertexType is VertexPosition)
                return AllShaders.Basic_PositionTexture.Get(caps);
            return AllShaders.Basic_PositionColor.Get(caps);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct BasicParameters
        {
            public Color4 Dc;
            public Color4 Ec;
            public Vector2 FadeRange;
            public float Oc;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct PBRParameters
        {
            public Color4 Dc;
            public Color4 Ec;
            public float Oc;
            public float Roughness;
            public float Metallic;
        }

        public override void Use(RenderContext rstate, IVertexType vertextype, ref Lighting lights, int userData)
		{
            bool pbr = (!string.IsNullOrWhiteSpace(RtSampler) ||
                       !string.IsNullOrWhiteSpace(MtSampler) || Metallic != null || Roughness != null) &&
                       lights.Enabled;
            var dxt1 = GetDxt1();

			var shader = pbr ? GetPBRShader(vertextype) : GetRegularShader(vertextype);
            SetWorld(shader);
            //Dt
			BindTexture(rstate, 0, DtSampler, 0, DtFlags, ResourceManager.WhiteTextureName);
			//Dc
            var dcValue = Dc;
            if ((userData & DcSet) == DcSet)
            {
                var d = (VertexDiffuse)(uint)userData;
                d.A = 255;
                dcValue = (Color4)d;
            }
            //Blending
			if (AlphaEnabled || Fade || OcEnabled || dxt1 || AlphaTest || (userData & ForceAlpha) == ForceAlpha)
			{
				rstate.BlendMode = BlendMode.Normal;
			}
			else
			{
                rstate.BlendMode = BlendMode.Opaque; //TODO: Maybe I can just leave this out?
			}
			//MaterialAnim
            var ma = new Vector4(0, 0, 1, 1);
			if (MaterialAnim != null)
			{
                ma = new Vector4(
                    MaterialAnim.UOffset,
                    MaterialAnim.VOffset,
                    MaterialAnim.UScale,
                    MaterialAnim.VScale
                );
			}
			shader.SetUniformBlock(4, ref ma);

            if (Bones != null && vertextype is DfmVertex) {
                Bones.BindTo(9, BufferOffset, 200);
            }

            //EtSampler
			if (EtEnabled)
			{
				BindTexture(rstate, 1, EtSampler, 1, EtFlags, ResourceManager.NullTextureName);
			}
            if (!string.IsNullOrEmpty(NmSampler))
            {
                BindTexture(rstate, 2, NmSampler, 2, NmFlags, ResourceManager.NullTextureName);
            }

            if (pbr)
            {
                var param = new PBRParameters()
                    { Dc = dcValue, Ec = Ec, Metallic = Metallic ?? 1.0f, Roughness = Roughness ?? 1.0f, Oc = Oc };
                shader.SetUniformBlock(3, ref param);
                if (!string.IsNullOrEmpty(MtSampler))
                {
                    BindTexture(rstate, 3, MtSampler, 3, MtFlags, ResourceManager.WhiteTextureName);
                }
                if (!string.IsNullOrEmpty(RtSampler))
                {
                    BindTexture(rstate, 4, RtSampler, 4, RtFlags, ResourceManager.WhiteTextureName);
                }
                SetTextureCoordinates(shader, DtFlags, EtFlags, NmFlags, MtFlags, RtFlags);
            }
            else
            {
                var param = new BasicParameters()
                    { Dc = dcValue, Ec = Ec, FadeRange = new Vector2(FadeNear, FadeFar), Oc = Oc };
                shader.SetUniformBlock(3, ref param);
                SetTextureCoordinates(shader, DtFlags, EtFlags, NmFlags);
            }
			//Set lights
            SetLights(shader, ref lights, rstate.FrameNumber);
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

