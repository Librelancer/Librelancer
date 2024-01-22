// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Utf.Mat;

namespace LibreLancer.Render.Materials
{
    public class ZoneVolumeMaterial : RenderMaterial
    {
        public Color4 Dc = Color4.White;
        public float RadiusRatio;

        public ZoneVolumeMaterial(ResourceManager library) : base(library) { }

        public override void ApplyDepthPrepass(RenderContext rstate)
        {
            throw new InvalidOperationException();
        }

        public override bool IsTransparent
        {
            get
            {
                return true;
            }
        }

        public override bool DisableCull => true;

        public override void Use(RenderContext rstate, IVertexType vertextype, ref Lighting lights, int userData)
        {
            rstate.BlendMode = BlendMode.Normal;
            var shader = Shaders.ZoneVolume.Get(rstate);
            shader.SetWorld(World);
            //Colors
            shader.SetDc(Dc);
            //Dt
            shader.SetTileRate0(RadiusRatio);
            rstate.Shader = shader;
        }
    }
}
