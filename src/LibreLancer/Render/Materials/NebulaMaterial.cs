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
        public Color4 Dc = Color4.White;
        public NebulaMaterial(ResourceManager library) : base(library) { }

		Shader GetShader(IVertexType vtype)
        {
            if (vtype is FVFVertex fvf && fvf.Diffuse)
                return AllShaders.NebulaMaterial.Get(1);
            return AllShaders.NebulaMaterial.Get(0);
        }


		public override void Use(RenderContext rstate, IVertexType vertextype, ref Lighting lights, int userData)
		{
            //fragment shader you multiply tex sampler rgb by vertex color and alpha the same (that is should texture have alpha of its own, sometimes they may as well)
			rstate.BlendMode = BlendMode.Additive;
			var shader = GetShader(vertextype);
            shader.SetUniformBlock(3, ref Dc);
			SetWorld(shader);
			//Dt
			BindTexture (rstate, 0, DtSampler, 0, DtFlags);
            rstate.Shader = shader;
        }


		public override bool IsTransparent => true;
    }
}

