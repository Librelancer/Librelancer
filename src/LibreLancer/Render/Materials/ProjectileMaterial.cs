using System;
using System.Numerics;
using LibreLancer.Shaders;
using LibreLancer.Utf.Mat;
using LibreLancer.Vertices;

namespace LibreLancer.Render.Materials;

public class ProjectileMaterial : RenderMaterial
{
    private static ShaderVariables shader;

    static ProjectileMaterial()
    {
        shader = Shaders.Projectile.Get();
    }
    
    public ProjectileMaterial(ILibFile library) : base(library) { }


    public override void Use(RenderContext rstate, IVertexType vertextype, ref Lighting lights, int userData)
    {
        shader.SetDtSampler(0);
        BindTexture(rstate, 0, "code_beam", 0, SamplerFlags.ClampToEdgeU | SamplerFlags.ClampToEdgeV);
        rstate.BlendMode = BlendMode.Additive;
        shader.UseProgram();
    }

    public override bool IsTransparent => true;

    public override bool DisableCull => true;

    public override void ApplyDepthPrepass(RenderContext rstate)
    {
        throw new InvalidOperationException();
    }
}