using System;
using System.Numerics;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Shaders;
using LibreLancer.Utf.Mat;

namespace LibreLancer.Render.Materials;

public class ProjectileMaterial : RenderMaterial
{
    public ProjectileMaterial(ResourceManager library) : base(library) { }


    public override void Use(RenderContext rstate, IVertexType vertextype, ref Lighting lights, int userData)
    {
        var shader = Shaders.Projectile.Get(rstate);
        shader.SetDtSampler(0);
        BindTexture(rstate, 0, "code_beam", 0, SamplerFlags.ClampToEdgeU | SamplerFlags.ClampToEdgeV);
        rstate.BlendMode = BlendMode.Additive;
        rstate.Shader = shader;
    }

    public override bool IsTransparent => true;

    public override bool DisableCull => true;

    public override void ApplyDepthPrepass(RenderContext rstate)
    {
        throw new InvalidOperationException();
    }
}
