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

		static ShaderVariables sh_posNormalTexture;
		static ShaderVariables sh_posNormalTextureTwo;
		static ShaderVariables sh_posNormalColorTexture;
		static ShaderVariables sh_posTexture;
		static ShaderVariables sh_pos;
		static ShaderVariables GetShader(IVertexType vertextype, ShaderCaps caps)
		{
			if (vertextype is VertexPositionNormalTexture)
			{
				if (sh_posNormalTexture == null)
					sh_posNormalTexture = ShaderCache.Get(
						"Basic_PositionNormalTexture.vs",
						"Basic_Fragment.frag",
						caps
					);
				return sh_posNormalTexture;
			}
			if (vertextype is VertexPositionNormalTextureTwo)
			{
				if (sh_posNormalTextureTwo == null)
					sh_posNormalTextureTwo = ShaderCache.Get(
						"Basic_PositionNormalTextureTwo.vs",
						"Basic_Fragment.frag",
						caps
					);
				return sh_posNormalTextureTwo;
			}
			if (vertextype is VertexPositionNormalColorTexture)
			{
				if (sh_posNormalColorTexture == null)
					sh_posNormalColorTexture = ShaderCache.Get(
						"Basic_PositionNormalColorTexture.vs",
						"Basic_Fragment.frag",
						caps
					);
				return sh_posNormalColorTexture;
			}
			if (vertextype is VertexPositionTexture)
			{
				if (sh_posTexture == null)
					sh_posTexture = ShaderCache.Get(
						"Basic_PositionTexture.vs",
						"Basic_Fragment.frag",
						caps
					);
				return sh_posTexture;
			}
			if (vertextype is VertexPosition)
			{
				if (sh_pos == null)
					sh_pos = ShaderCache.Get(
						"Basic_PositionTexture.vs",
						"Basic_Fragment.frag",
						caps
					);
				return sh_pos;
			}
			throw new NotImplementedException(vertextype.GetType().Name);
		}
		public override void Use(RenderState rstate, IVertexType vertextype, Lighting lights)
		{
			if (Camera == null)
				return;
			ShaderCaps caps = ShaderCaps.None;
			if (EtEnabled) caps |= ShaderCaps.EtEnabled;
			if (Fade) caps |= ShaderCaps.FadeEnabled;
			if (AlphaEnabled && GetTexture(0, DtSampler).Dxt1) caps |= ShaderCaps.AlphaTestEnabled;
			var shader = GetShader(vertextype, caps);
			shader.SetWorld(ref World);
			shader.SetView(Camera);
			shader.SetViewProjection(Camera);
			//Dt
			shader.SetDtSampler(0);
			BindTexture(rstate, 0, DtSampler, 0, DtFlags, false);
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
				BindTexture(rstate, 1, EtSampler, 1, EtFlags, false);
			}
			//Set lights
			SetLights(shader, lights);
			var normalMatrix = World;
			normalMatrix.Invert();
			normalMatrix.Transpose();
			shader.SetNormalMatrix(ref normalMatrix);
			shader.UseProgram();
		}
		public override bool IsTransparent
		{
			get
			{
				return AlphaEnabled && !GetTexture(0, DtSampler).Dxt1;
			}
		}
	}
}

