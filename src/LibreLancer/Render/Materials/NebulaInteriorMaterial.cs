using System;
using System.Numerics;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Shaders;
using LibreLancer.Utf.Mat;

namespace LibreLancer.Render.Materials;

public class NebulaInteriorMaterial : RenderMaterial
{
    public string Texture;
    public Color4 Dc;

    public NebulaInteriorMaterial(ResourceManager library) : base(library) { }


    public override void Use(RenderContext rstate, IVertexType vertextype, ref Lighting lights, int userData)
    {
        var shader = Shaders.NebulaInterior.Get(rstate);
        shader.SetWorld(World);
        shader.SetDtSampler(0);
        shader.SetDc(Dc);
        BindTexture(rstate, 0, Texture, 0, SamplerFlags.Default);
        rstate.BlendMode = BlendMode.Normal;
        rstate.Shader = shader;
    }

    public override bool IsTransparent => true;
    public override bool DisableCull => true;

    public override void ApplyDepthPrepass(RenderContext rstate)
    {
        throw new InvalidOperationException();
    }
}
