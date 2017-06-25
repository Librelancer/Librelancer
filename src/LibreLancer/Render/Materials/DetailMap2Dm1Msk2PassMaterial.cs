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
	public class DetailMap2Dm1Msk2PassMaterial : RenderMaterial
	{
		public Color4 Ac = Color4.White;
		public Color4 Dc = Color4.White;
		public string DtSampler;
		public SamplerFlags DtFlags;
		public string Dm1Sampler;
		public SamplerFlags Dm1Flags;
		public int FlipU;
		public int FlipV;
		public float TileRate;

		public DetailMap2Dm1Msk2PassMaterial ()
		{
		}
        static ShaderVariables sh_posNormalTexture;
		static ShaderVariables GetShader(IVertexType vertextype)
		{
			if (vertextype is VertexPositionNormalTexture) {
                if(sh_posNormalTexture == null)
				sh_posNormalTexture = ShaderCache.Get (
					"PositionTextureFlip.vs",
					"DetailMap2Dm1Msk2PassMaterial.frag"
				);
                return sh_posNormalTexture;
			}
			throw new NotImplementedException ();
		}
		public override void Use (RenderState rstate, IVertexType vertextype, Lighting lights)
		{
			rstate.DepthEnabled = true;
			rstate.BlendMode = BlendMode.Opaque;

			ShaderVariables sh = GetShader (vertextype);
			sh.SetWorld (ref World);
			sh.SetViewProjection (Camera);
			sh.SetView (Camera);
			sh.SetAc(Ac);
			sh.SetDc(Dc);
			sh.SetTileRate(TileRate);
			sh.SetFlipU(FlipU);
			sh.SetFlipV(FlipV);

			sh.SetDtSampler(0);
			BindTexture (rstate ,0, DtSampler, 0, DtFlags);
			sh.SetDm1Sampler(1);
			BindTexture (rstate, 1, Dm1Sampler, 1, Dm1Flags);
			SetLights(sh, lights);
			var normalMatrix = World;
			normalMatrix.Invert();
			normalMatrix.Transpose();
			sh.SetNormalMatrix(ref normalMatrix);
			sh.UseProgram ();
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

