// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Vertices;
using LibreLancer;
using LibreLancer.Shaders;
using LibreLancer.Render;

namespace LancerEdit.Materials
{
    public class CubemapMaterial : RenderMaterial
    {
        public TextureCube Texture;

        public CubemapMaterial(ILibFile library) : base(library) { }

        public override void Use(RenderContext rstate, IVertexType vertextype, ref Lighting lights, int userData)
        {
            var sh = EnvMapTest.Get();
            sh.SetDtSampler(0);
            Texture.BindTo(0);
            rstate.BlendMode = BlendMode.Opaque;
            //Dt
            sh.SetWorld(World);
            sh.UseProgram();
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