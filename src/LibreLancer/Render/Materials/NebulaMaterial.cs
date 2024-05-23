// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Shaders;
using LibreLancer.Utf.Mat;
using LibreLancer.Utf.Vms;

namespace LibreLancer.Render.Materials
{
	public class NebulaMaterial : RenderMaterial
	{
		public string DtSampler;
		public SamplerFlags DtFlags;
        public NebulaMaterial(ResourceManager library) : base(library) { }

		ShaderVariables GetShader(RenderContext rstate, IVertexType vtype)
		{
            if (vtype is FVFVertex fvf && fvf.Diffuse)
                return Shaders.NebulaMaterial.Get(rstate, ShaderFeatures.VERTEX_DIFFUSE);
            return Shaders.NebulaMaterial.Get(rstate);
        }
		public override void Use(RenderContext rstate, IVertexType vertextype, ref Lighting lights, int userData)
		{
            //fragment shader you multiply tex sampler rgb by vertex color and alpha the same (that is should texture have alpha of its own, sometimes they may as well)
			rstate.BlendMode = BlendMode.Additive;
			var shader = GetShader(rstate, vertextype);
			shader.SetWorld(World);
			//Dt
			shader.SetDtSampler(0);
			BindTexture (rstate, 0, DtSampler, 0, DtFlags);
            rstate.Shader = shader;
        }

		public override void ApplyDepthPrepass(RenderContext rstate)
		{
			throw new InvalidOperationException();
		}

		public override bool IsTransparent => true;
    }
}

