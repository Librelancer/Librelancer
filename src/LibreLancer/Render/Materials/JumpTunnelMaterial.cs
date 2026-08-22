// MIT License - Copyright (c) LibreLancer contributors
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;
using System.Runtime.InteropServices;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Resources;
using LibreLancer.Shaders;
using LibreLancer.Utf.Mat;

namespace LibreLancer.Render.Materials;

internal sealed class JumpTunnelMaterial(ResourceManager library) : RenderMaterial(library)
{
    public string? Texture;
    public bool WriteDepth;
    public float Du;
    public float Dv;
    public float Elapsed;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct TunnelParameters
    {
        public Vector2 Scroll;
        public float Opacity;
        private float padding;
    }

    public override void Use(RenderContext rstate, IVertexType vertextype, ref Lighting lights, int userData)
    {
        var shader = AllShaders.JumpTunnel.Get(0);
        SetWorld(shader);
        var parameters = new TunnelParameters
        {
            Scroll = new Vector2(Du * Elapsed, Dv * Elapsed),
            Opacity = OpacityMultiplier
        };
        shader.SetUniformBlock(3, ref parameters);
        BindTexture(rstate, 0, Texture, 0, SamplerFlags.Default,
            ResourceManager.WhiteTextureName);
        rstate.BlendMode = BlendMode.Normal;
        rstate.Shader = shader;
    }

    public override bool IsTransparent => !WriteDepth;
    public override bool DisableCull => true;
}
