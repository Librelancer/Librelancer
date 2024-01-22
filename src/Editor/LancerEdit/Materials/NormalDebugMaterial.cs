// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Shaders;
using LibreLancer.Render;

namespace LancerEdit.Materials
{
	public class NormalDebugMaterial : RenderMaterial
	{

        public NormalDebugMaterial(ResourceManager library) : base(library) { }

        public override void Use(RenderContext rstate, IVertexType vertextype, ref Lighting lights, int userData)
		{
            ShaderVariables sh;
            //These things don't have normals
            if (vertextype is VertexPositionColorTexture ||
            vertextype is VertexPosition ||
                vertextype is VertexPositionColor || vertextype is VertexPositionTexture)
            {
                sh = DepthPass_Normal.Get(rstate);
            }
            else
            {
                sh = Normals.Get(rstate);
            }
            rstate.BlendMode = BlendMode.Opaque;
			//Dt
            sh.SetWorld(World);
            rstate.Shader = sh;
        }

		public override void ApplyDepthPrepass(RenderContext rstate)
		{
			throw new NotImplementedException();
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

