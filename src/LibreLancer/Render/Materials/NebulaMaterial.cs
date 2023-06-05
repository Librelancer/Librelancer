// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Shaders;
using LibreLancer.Utf.Mat;
using LibreLancer.Vertices;

namespace LibreLancer.Render.Materials
{
	public class NebulaMaterial : RenderMaterial
	{
		public string DtSampler;
		public SamplerFlags DtFlags;
        public NebulaMaterial(ILibFile library) : base(library) { }

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
		public override void Use(RenderContext rstate, IVertexType vertextype, ref Lighting lights, int userData)
		{
            //fragment shader you multiply tex sampler rgb by vertex color and alpha the same (that is should texture have alpha of its own, sometimes they may as well)
			rstate.BlendMode = BlendMode.Additive;
			var shader = GetShader(vertextype);
			shader.SetWorld(World);
			//Dt
			shader.SetDtSampler(0);
			BindTexture (rstate, 0, DtSampler, 0, DtFlags);
			shader.UseProgram ();
		}

		public override void ApplyDepthPrepass(RenderContext rstate)
		{
			throw new InvalidOperationException();
		}

		public override bool IsTransparent => true;
    }
}

