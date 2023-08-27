using System;
using System.Numerics;
using LibreLancer.Shaders;
using LibreLancer.Utf.Mat;
using LibreLancer.Vertices;

namespace LibreLancer.Render.Materials;

public class NebulaInteriorMaterial : RenderMaterial
{
    private static ShaderVariables shader;

    public string Texture;
    public Color4 Dc;

    public NebulaInteriorMaterial(ResourceManager library) : base(library) { }


    static NebulaInteriorMaterial()
    {
        shader = Shaders.NebulaInterior.Get();
    }

    public override void Use(RenderContext rstate, IVertexType vertextype, ref Lighting lights, int userData)
    {
        shader.SetWorld(World);
        shader.SetDtSampler(0);
        shader.SetDc(Dc);
        BindTexture(rstate, 0, Texture, 0, SamplerFlags.Default);
        rstate.BlendMode = BlendMode.Normal;
        shader.UseProgram();
    }

    public override bool IsTransparent => true;
    public override bool DisableCull => true;

    public override void ApplyDepthPrepass(RenderContext rstate)
    {
        throw new InvalidOperationException();
    }
}
