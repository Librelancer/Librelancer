// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Vertices;
using LibreLancer.Utf.Mat;

namespace LibreLancer
{
	public class NormalDebugMaterial : RenderMaterial
	{
        static ShaderVariables shader;
        static ShaderVariables shader_null;
		public override void Use(RenderState rstate, IVertexType vertextype, ref Lighting lights)
		{
            ShaderVariables sh;
            //These things don't have normals
            if (vertextype is VertexPositionColorTexture ||
            vertextype is VertexPosition ||
                vertextype is VertexPositionColor || vertextype is VertexPositionTexture)
            {
                if (shader_null == null)
                    shader_null = ShaderCache.Get("Normals_Position.vs", "Normals.frag");
                sh = shader_null;
            }
            else
            {
                if (shader == null)
                    shader = ShaderCache.Get("Normals_PositionNormal.vs", "Normals.frag");
                sh = shader;
            }
			if (Camera == null)
				return;
			rstate.BlendMode = BlendMode.Opaque;
			sh.SetViewProjection(Camera);
			//Dt
			var normalMatrix = World;
			normalMatrix.Invert();
			normalMatrix.Transpose();
			sh.SetWorld(ref World);
			sh.SetNormalMatrix(ref normalMatrix);
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

