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
	public class DetailMapMaterial : RenderMaterial
	{
		public string DmSampler;
		public SamplerFlags DmFlags;
		public float TileRate;
		public int FlipU;
		public int FlipV;
		public Color4 Ac;
		public Color4 Dc;
		public string DtSampler;
		public SamplerFlags DtFlags;

        static ShaderVariables sh_posNormalTexture;
		static ShaderVariables GetShader(IVertexType vertextype) {
			if (vertextype.GetType ().Name == "VertexPositionNormalTexture") {
                if(sh_posNormalTexture == null)
				return ShaderCache.Get (
					"PositionTextureFlip.vs",
					"DetailMapMaterial.frag"
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
			sh.SetView (ref View);
			sh.SetViewProjection (ref ViewProjection);

			sh.SetAc(Ac);
			sh.SetDc(Dc);
			sh.SetTileRate(TileRate);
			sh.SetFlipU(FlipU);
			sh.SetFlipV(FlipV);

			sh.SetDtSampler(0);
			BindTexture (0, DtSampler, 0, DtFlags);
			sh.SetDmSampler(1);
			BindTexture (1, DmSampler, 1, DmFlags);
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

