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
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using LibreLancer.Vertices;
using LibreLancer.Utf.Mat;
namespace LibreLancer
{
	public class IllumDetailMapMaterial : RenderMaterial
	{
		public Color4 Ac = Color4.White;
		public Color4 Dc = Color4.White;
		public Texture DtSampler;
		public SamplerFlags DtFlags;
		public Texture Dm0Sampler;
		public SamplerFlags Dm0Flags;
		public Texture Dm1Sampler;
		public SamplerFlags Dm1Flags;
		public float TileRate0;
		public float TileRate1;
		public int FlipU;
		public int FlipV;

		Shader GetShader(IVertexType vertextype)
		{
			if (vertextype.GetType().Name == "VertexPositionTexture")
			{
				return ShaderCache.Get(
					"PositionTextureFlip.vs",
					"IllumDetailMapMaterial.frag"
				);
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
			sh.SetMatrix("ViewProjection", ref ViewProjection);
			sh.SetMatrix("World", ref World);

			sh.SetColor4("Ac", Ac);
			sh.SetColor4("Dc", Dc);
			sh.SetFloat("TileRate0", TileRate0);
			sh.SetFloat("TileRate1", TileRate1);
			sh.SetInteger("FlipU", FlipU);
			sh.SetInteger("FlipV", FlipV);

			sh.SetInteger("DtSampler", 0);
			BindTexture(DtSampler, TextureUnit.Texture0, DtFlags);
			sh.SetInteger("Dm0Sampler", 1);
			BindTexture(Dm0Sampler, TextureUnit.Texture1, Dm0Flags);
			sh.SetInteger("Dm1Sampler", 2);
			BindTexture(Dm1Sampler, TextureUnit.Texture2, Dm1Flags);

			sh.UseProgram();
		}
	}
}

