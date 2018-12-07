// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Vertices;
using LibreLancer.Utf.Mat;
namespace LibreLancer
{
	public class NebulaMaterial : RenderMaterial
	{
		public string DtSampler;
		public SamplerFlags DtFlags;
		public NebulaMaterial ()
		{
		}
		ShaderVariables GetShader(IVertexType vtype)
		{
			switch (vtype.GetType ().Name) {
			case "VertexPositionNormalDiffuseTexture":
				return ShaderCache.Get (
					"PositionColorTexture.vs",
					"Nebula_PositionColorTexture.frag"
				);
			case "VertexPositionNormalTexture":
				return ShaderCache.Get(
					"PositionTextureFlip.vs",
					"Nebula_PositionColorTexture.frag"
				);
			default:
				throw new NotImplementedException ();
			}
		}
		public override void Use(RenderState rstate, IVertexType vertextype, ref Lighting lights)
		{
            if (Camera == null)
                return;
			//fragment shader you multiply tex sampler rgb by vertex color and alpha the same (that is should texture have alpha of its own, sometimes they may as well)
			rstate.BlendMode = BlendMode.Additive;
			var shader = GetShader(vertextype);
			shader.SetWorld(ref World);
            shader.SetViewProjection(Camera);
			//Dt
			shader.SetDtSampler(0);
			BindTexture (rstate, 0, DtSampler, 0, DtFlags);
			shader.UseProgram ();
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
	}
}

