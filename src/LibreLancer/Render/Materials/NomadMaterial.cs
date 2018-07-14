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
 * Portions created by the Initial Developer are Copyright (C) 2013-2017
 * the Initial Developer. All Rights Reserved.
 */
using System;
using LibreLancer.Vertices;
using LibreLancer.Utf.Mat;
namespace LibreLancer
{
	public class NomadMaterial : RenderMaterial
	{
		public Color4 Dc = Color4.White;
		public string DtSampler;
		public SamplerFlags DtFlags;

		public string BtSampler;
		public SamplerFlags BtFlags;

		public string NtSampler;
		public SamplerFlags NtFlags;

		public float Oc = 1f;

		public NomadMaterial()
		{
		}

		static ShaderVariables sh_one;
		static ShaderVariables sh_two;
		static ShaderVariables GetShader(IVertexType vertexType)
		{
			if (vertexType is VertexPositionNormalTextureTwo)
			{
				if (sh_two == null)
					sh_two = ShaderCache.Get("Basic_PositionNormalTextureTwo.vs", "NomadMaterial.frag");
				return sh_two;
			}
			else if (vertexType is VertexPositionNormalTexture)
			{
				if (sh_one == null)
					sh_one = ShaderCache.Get("Nomad_PositionNormalTexture.vs", "NomadMaterial.frag");
				return sh_one;
			}
			throw new NotImplementedException(vertexType.GetType().Name);
		}


		public override void ApplyDepthPrepass(RenderState rstate)
		{
			throw new InvalidOperationException();
		}

		public override bool IsTransparent
		{
			get
			{
				return true;
			}
		}

		public override void Use(RenderState rstate, IVertexType vertextype, ref Lighting lights)
		{
			rstate.BlendMode = BlendMode.Additive;
			var shader = GetShader(vertextype);
			shader.SetWorld(ref World);
			shader.SetView(Camera);
			shader.SetViewProjection(Camera);
			//Colors
			shader.SetDc(Dc);
			shader.SetOc(Oc);
			//Dt
			shader.SetDtSampler(0);
			BindTexture(rstate, 0, DtSampler, 0, DtFlags);
			//Nt
			shader.SetDmSampler(1); //Repurpose DmSampler
			BindTexture(rstate, 1, NtSampler ?? "NomadRGB1_NomadAlpha1", 1, NtFlags);
			//Bt

			//Disable MaterialAnim
			shader.SetMaterialAnim(new Vector4(0, 0, 1, 1));
			shader.UseProgram();
		}
	}
}
