// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Shaders;
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
            if (vtype is VertexPositionNormalDiffuseTexture ||
                vtype is VertexPositionNormalDiffuseTextureTwo)
            {
                return Shaders.NebulaMaterial.Get(ShaderFeatures.VERTEX_DIFFUSE);
            } 
            if (vtype is VertexPositionNormalTexture ||
                       vtype is VertexPositionNormalTextureTwo)
            {
                return Shaders.NebulaMaterial.Get();
            }
            throw new NotImplementedException(vtype.GetType().ToString());
        }
		public override void Use(RenderState rstate, IVertexType vertextype, ref Lighting lights)
		{
            if (Camera == null)
                return;
			//fragment shader you multiply tex sampler rgb by vertex color and alpha the same (that is should texture have alpha of its own, sometimes they may as well)
			rstate.BlendMode = BlendMode.Additive;
			var shader = GetShader(vertextype);
			shader.SetWorld(World);
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

		public override bool IsTransparent => true;
    }
}

