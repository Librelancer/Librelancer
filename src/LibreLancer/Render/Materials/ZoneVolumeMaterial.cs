// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Runtime.InteropServices;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Shaders;
using LibreLancer.Utf.Mat;

namespace LibreLancer.Render.Materials
{
    public class ZoneVolumeMaterial : RenderMaterial
    {
        public Color4 Dc = Color4.White;
        public float RadiusRatio;

        public ZoneVolumeMaterial(ResourceManager library) : base(library) { }

        public override bool IsTransparent
        {
            get
            {
                return true;
            }
        }

        public override bool DisableCull => true;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct ZoneVolumeParameters
        {
            public Color4 Dc;
            public float RadiusRatio;
        }

        public override void Use(RenderContext rstate, IVertexType vertextype, ref Lighting lights, int userData)
        {
            rstate.BlendMode = BlendMode.Normal;
            var shader = AllShaders.ZoneVolume.Get(0);
            SetWorld(shader);
            shader.SetUniformBlock(3, ref RadiusRatio);
            shader.SetUniformBlock(4, ref Dc);
            rstate.Shader = shader;
        }
    }
}
