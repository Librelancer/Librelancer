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
	public class IllumDetailMapMaterial : RenderMaterial
	{
		public Color4 Ac = Color4.White;
		public Color4 Dc = Color4.White;
		public string DtSampler;
		public SamplerFlags DtFlags;
		public string Dm0Sampler;
		public SamplerFlags Dm0Flags;
		public string Dm1Sampler;
		public SamplerFlags Dm1Flags;
		public float TileRate0;
		public float TileRate1;
		public int FlipU;
		public int FlipV;

        static ShaderVariables sh_posNormalTexture;
		static ShaderVariables GetShader(IVertexType vertextype)
		{
			if (vertextype.GetType().Name == "VertexPositionNormalTexture")
			{
                if(sh_posNormalTexture == null)
				sh_posNormalTexture = ShaderCache.Get(
					"PositionTextureFlip.vs",
					"IllumDetailMapMaterial.frag"
				);
                return sh_posNormalTexture;
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		public override void Use(RenderState rstate, IVertexType vertextype, Lighting lights)
		{
			rstate.DepthEnabled = true;
			rstate.BlendMode = BlendMode.Opaque;

			var sh = GetShader(vertextype);
            sh.SetViewProjection(Camera);
			sh.SetWorld(ref World);
            sh.SetView(Camera);

			sh.SetAc(Ac);
			sh.SetDc(Dc);
			sh.SetTileRate0(TileRate0);
			sh.SetTileRate1(TileRate1);
			sh.SetFlipU(FlipU);
			sh.SetFlipV(FlipV);

			sh.SetDtSampler(0);
			BindTexture(0, DtSampler, 0, DtFlags);
			sh.SetDm0Sampler(1);
			BindTexture(1, Dm0Sampler, 1, Dm0Flags);
			sh.SetDm1Sampler(2);
			BindTexture(2, Dm1Sampler, 2, Dm1Flags);
			SetLights(sh, lights);
			var normalMatrix = World;
			normalMatrix.Invert();
			normalMatrix.Transpose();
			sh.SetNormalMatrix(ref normalMatrix);
			sh.UseProgram();
		}
		public override bool IsTransparent
		{
			get
			{
				return false;
			}
		}
	}
}

