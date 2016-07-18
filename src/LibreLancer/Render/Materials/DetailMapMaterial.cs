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

		Shader GetShader(IVertexType vertextype) {
			if (vertextype.GetType ().Name == "VertexPositionNormalTexture") {
				return ShaderCache.Get (
					"PositionTextureFlip.vs",
					"DetailMapMaterial.frag"
				);
			}
			throw new NotImplementedException ();
		}

		public override void Use (RenderState rstate, IVertexType vertextype, Lighting lights)
		{
			rstate.DepthEnabled = true;
			rstate.BlendMode = BlendMode.Opaque;

			Shader sh = GetShader (vertextype);
			sh.SetMatrix ("World", ref World);
			sh.SetMatrix ("View", ref View);
			sh.SetMatrix ("ViewProjection", ref ViewProjection);
			sh.SetMatrix("View", ref View);

			sh.SetColor4 ("Ac", Ac);
			sh.SetColor4 ("Dc", Dc);
			sh.SetFloat ("TileRate", TileRate);
			sh.SetInteger ("FlipU", FlipU);
			sh.SetInteger ("FlipV", FlipV);

			sh.SetInteger ("DtSampler", 0);
			BindTexture (DtSampler, 0, DtFlags);
			sh.SetInteger ("DmSampler", 1);
			BindTexture (DmSampler, 1, DmFlags);
			SetLights(sh, lights);
			var normalMatrix = World;
			normalMatrix.Invert();
			normalMatrix.Transpose();
			sh.SetMatrix("NormalMatrix", ref normalMatrix);
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

