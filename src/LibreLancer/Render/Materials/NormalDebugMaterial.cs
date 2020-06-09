// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Vertices;

namespace LibreLancer
{
	public class NormalDebugMaterial : RenderMaterial
	{
        public override void Use(RenderState rstate, IVertexType vertextype, ref Lighting lights)
		{
            Shaders.ShaderVariables sh;
            //These things don't have normals
            if (vertextype is VertexPositionColorTexture ||
            vertextype is VertexPosition ||
                vertextype is VertexPositionColor || vertextype is VertexPositionTexture)
            {
                sh = Shaders.DepthPass_Normal.Get();
            }
            else
            {
                sh = Shaders.Normals.Get();
            }
			if (Camera == null)
				return;
			rstate.BlendMode = BlendMode.Opaque;
			sh.SetViewProjection(Camera);
			//Dt
            sh.SetWorld(World);
            sh.UseProgram();
		}

		public override void ApplyDepthPrepass(RenderState rstate)
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

