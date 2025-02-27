// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LancerEdit.Shaders;
using LibreLancer;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Shaders;
using LibreLancer.Render;

namespace LancerEdit.Materials
{
    public class CubemapMaterial : RenderMaterial
    {
        public TextureCube Texture;

        public CubemapMaterial(ResourceManager library) : base(library) { }

        public override void Use(RenderContext rstate, IVertexType vertextype, ref Lighting lights, int userData)
        {
            var sh = EditorShaders.EnvMapTest.Get(0);
            Texture.BindTo(0);
            rstate.BlendMode = BlendMode.Opaque;
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
