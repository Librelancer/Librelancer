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
		static ShaderVariables GetShader(IVertexType vertextype, ShaderCaps caps)
		{
			var i = caps.GetIndex();
			if (vertextype is VertexPositionNormalTexture || vertextype is Utf.Dfm.DfmVertex)
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
			if (vertextype is VertexPositionNormalColorTexture)
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
			throw new NotImplementedException(vertextype.GetType().Name);
		}
		public override void Use(RenderState rstate, IVertexType vertextype, Lighting lights)
		{
			if (Camera == null)
				return;
			ShaderCaps caps = ShaderCaps.None;
			if (HasSpotlight(ref lights)) caps |= ShaderCaps.Spotlight;
			if (EtEnabled) caps |= ShaderCaps.EtEnabled;
			if (Fade) caps |= ShaderCaps.FadeEnabled;
			var dt = GetTexture(0, DtSampler);
			if (dt != null && dt.Dxt1)
			{
				caps |= ShaderCaps.AlphaTestEnabled; //Shitty way of dealing with alpha_mask
			}
			var shader = GetShader(vertextype, caps);
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
			if (AlphaEnabled || Fade || OcEnabled)
			{
				rstate.BlendMode = BlendMode.Normal;
			}
			else
			{
				rstate.BlendMode = BlendMode.Opaque;
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
			SetLights(shader, lights);
			var normalMatrix = World;
			normalMatrix.Invert();
			normalMatrix.Transpose();
			shader.SetNormalMatrix(ref normalMatrix);
			shader.UseProgram();
		}

		public override void ApplyDepthPrepass(RenderState rstate)
		{
			rstate.BlendMode = BlendMode.Normal;
			/*var tex = GetTexture(0, DtSampler);
			ShaderVariables shader;
			if (tex.Dxt1)
			{
				shader = AlphaTestPrepassShader;
				shader.SetDtSampler(0);
				BindTexture(rstate, 0, DtSampler, 0, DtFlags, ResourceManager.WhiteTextureName);
			}
			else
				shader = NormalPrepassShader;*/
			var shader = NormalPrepassShader;
			shader.SetWorld(ref World);
			shader.SetViewProjection(Camera);
			shader.UseProgram();
		}

		public override bool IsTransparent
		{
			get
			{
				return AlphaEnabled;
			}
		}
	}
}

