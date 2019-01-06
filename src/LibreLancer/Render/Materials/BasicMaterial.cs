// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Vertices;
using LibreLancer.Utf.Mat;


namespace LibreLancer
{
	public class BasicMaterial : RenderMaterial
	{
		public string Type;

		public Color4 Dc = Color4.White;
		public string DtSampler;
		public SamplerFlags DtFlags;
		public float Oc = 1f;
		public bool OcEnabled = false;
		public bool EtEnabled = false;
		public bool AlphaEnabled = false;
		public Color4 Ec = Color4.White;
		public string EtSampler;
		public SamplerFlags EtFlags;

		public BasicMaterial(string type)
		{
			Type = type;
		}

		static ShaderVariables[] sh_posNormalTexture = new ShaderVariables[ShaderCapsExtensions.N_SHADERCAPS];
		static ShaderVariables[] sh_posNormalTextureTwo = new ShaderVariables[ShaderCapsExtensions.N_SHADERCAPS];
		static ShaderVariables[] sh_posNormalColorTexture = new ShaderVariables[ShaderCapsExtensions.N_SHADERCAPS];
		static ShaderVariables[] sh_posTexture = new ShaderVariables[ShaderCapsExtensions.N_SHADERCAPS];
		static ShaderVariables[] sh_pos = new ShaderVariables[ShaderCapsExtensions.N_SHADERCAPS];
        static ShaderVariables[] sh_posColor = new ShaderVariables[ShaderCapsExtensions.N_SHADERCAPS];
		static ShaderVariables GetShader(IVertexType vertextype, ShaderCaps caps)
		{
			var i = caps.GetIndex();
			if (vertextype is VertexPositionNormalTexture || vertextype is Utf.Dfm.DfmVertex ||
               vertextype is VertexPositionNormal)
			{
				if (sh_posNormalTexture[i] == null)
					sh_posNormalTexture[i] = ShaderCache.Get(
						"Basic_PositionNormalTexture.vs",
						"Basic_Fragment.frag",
						caps
					);
				return sh_posNormalTexture[i];
			}
			if (vertextype is VertexPositionNormalTextureTwo)
			{
				if (sh_posNormalTextureTwo[i] == null)
					sh_posNormalTextureTwo[i] = ShaderCache.Get(
						"Basic_PositionNormalTextureTwo.vs",
						"Basic_Fragment.frag",
						caps
					);
				return sh_posNormalTextureTwo[i];
			}
			if (vertextype is VertexPositionNormalDiffuseTexture)
			{
				if (sh_posNormalColorTexture[i] == null)
					sh_posNormalColorTexture[i] = ShaderCache.Get(
						"Basic_PositionNormalColorTexture.vs",
						"Basic_Fragment.frag",
						caps
					);
				return sh_posNormalColorTexture[i];
			}
			if (vertextype is VertexPositionTexture)
			{
				if (sh_posTexture[i] == null)
					sh_posTexture[i] = ShaderCache.Get(
						"Basic_PositionTexture.vs",
						"Basic_Fragment.frag",
						caps
					);
				return sh_posTexture[i];
			}
			if (vertextype is VertexPosition)
			{
				if (sh_pos[i] == null)
					sh_pos[i] = ShaderCache.Get(
						"Basic_PositionTexture.vs",
						"Basic_Fragment.frag",
						caps
					);
				return sh_pos[i];
			}
            if(vertextype is VertexPositionColor)
            {
                if (sh_posColor[i] == null)
                    sh_posColor[i] = ShaderCache.Get(
                        "Basic_PositionColor.vs",
                        "Basic_Fragment.frag",
                        caps
                    );
                return sh_posColor[i];
            }
			throw new NotImplementedException(vertextype.GetType().Name);
		}
        ShaderVariables lastShader;
        public override void UpdateFlipNormals()
        {
            lastShader.SetFlipNormal(FlipNormals);
        }
		public override void Use(RenderState rstate, IVertexType vertextype, ref Lighting lights)
		{
			if (Camera == null)
				return;
			ShaderCaps caps = ShaderCaps.None;
            if (VertexLighting) caps |= ShaderCaps.VertexLighting;
			if (HasSpotlight(ref lights)) caps |= ShaderCaps.Spotlight;
			if (EtEnabled) caps |= ShaderCaps.EtEnabled;
			if (Fade) caps |= ShaderCaps.FadeEnabled;
            var dxt1 = GetDxt1();
            if (dxt1)
			{
				caps |= ShaderCaps.AlphaTestEnabled; 
                //Shitty way of dealing with alpha_mask
                //FL has a lot of DXT1 textures that aren't part of alpha_mask
                //so this brings overall performance down.
                //Don't change any of this stuff unless you can verify it works
                //in all places! (Check Li01 shipyards, Bw10 tradelanes)
			}
			var shader = GetShader(vertextype, caps);
            lastShader = shader;
			shader.SetWorld(ref World);
			shader.SetView(Camera);
			shader.SetViewProjection(Camera);
			//Dt
			shader.SetDtSampler(0);
			BindTexture(rstate, 0, DtSampler, 0, DtFlags, ResourceManager.WhiteTextureName);
			//Dc
			shader.SetDc(Dc);
			//Oc
			shader.SetOc(Oc);
			if (AlphaEnabled || Fade || OcEnabled || dxt1)
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
			shader.SetFlipNormal(FlipNormals);
			//Ec
			shader.SetEc(Ec);
			//EtSampler
			if (EtEnabled)
			{
				shader.SetEtSampler(1);
				BindTexture(rstate, 1, EtSampler, 1, EtFlags, ResourceManager.NullTextureName);
			}
			//Set lights
			SetLights(shader, ref lights);
			var normalMatrix = World;
			normalMatrix.Invert();
			normalMatrix.Transpose();
			shader.SetNormalMatrix(ref normalMatrix);
			shader.UseProgram();
		}

		public override void ApplyDepthPrepass(RenderState rstate)
		{
			rstate.BlendMode = BlendMode.Normal;
            //TODO: This is screwy - Re-do DXT1 test if need be for perf
			var shader = AlphaTestPrepassShader;
            BindTexture(rstate, 0, DtSampler, 0, DtFlags, ResourceManager.WhiteTextureName);
			shader.SetWorld(ref World);
			shader.SetViewProjection(Camera);
			shader.UseProgram();
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

