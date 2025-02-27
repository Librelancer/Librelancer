// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Runtime.InteropServices;
using LancerEdit.Shaders;
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

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct Transforms
        {
            public Matrix4x4 World;
            public Matrix4x4 NormalMatrix;
        }

        public override void Use(RenderContext rstate, IVertexType vertextype, ref Lighting lights, int userData)
        {
            var sh = EditorShaders.Normals.Get(0);
            SetWorld(sh);
            rstate.Shader = sh;
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

