using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Shaders;
using LibreLancer.Utf.Mat;
using LibreLancer.Vertices;

namespace LibreLancer.Render.Materials;

public class ParticleMaterial : RenderMaterial
{
    private static ShaderVariables shader;

    public List<(Texture texture, BlendMode blendMode)> Parameters = new List<(Texture texture, BlendMode blendMode)>();

    static ParticleMaterial()
    {
        shader = Shaders.Particle.Get();
    }

    public ParticleMaterial(ResourceManager library) : base(library) { }


    public override void Use(RenderContext rstate, IVertexType vertextype, ref Lighting lights, int userData)
    {
        shader.SetDtSampler(0);
        Parameters[userData].texture.BindTo(0);
        rstate.BlendMode = Parameters[userData].blendMode;
        shader.UseProgram();
    }

    public override bool IsTransparent => true;

    public override bool DisableCull => true;

    public override void ApplyDepthPrepass(RenderContext rstate)
    {
        throw new InvalidOperationException();
    }
}
