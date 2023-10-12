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

    List<(Texture texture, int Flags)> parameters = new List<(Texture texture, int Flags)>();

    public int ParameterCount => parameters.Count;

    public void ResetParameters() => parameters.Clear();

    public int AddParameters(Texture texture, BlendMode blendMode, bool flipU, bool flipV)
    {
        int flags = (int) blendMode;
        if (flipU) flags |= 0x40000000;
        if (flipV) flags |= 0x20000000;
        parameters.Add((texture, flags));
        return parameters.Count - 1;
    }


    static ParticleMaterial()
    {
        shader = Shaders.Particle.Get();
    }

    public ParticleMaterial(ResourceManager library) : base(library) { }


    public override void Use(RenderContext rstate, IVertexType vertextype, ref Lighting lights, int userData)
    {
        shader.SetDtSampler(0);
        parameters[userData].texture.BindTo(0);
        rstate.BlendMode = (BlendMode)(parameters[userData].Flags & 0xFFFF);
        shader.SetFlipU((parameters[userData].Flags & 0x40000000) != 0 ? 1 : 0);
        shader.SetFlipV((parameters[userData].Flags & 0x20000000) != 0 ? 1 : 0);
        shader.UseProgram();
    }

    public override bool IsTransparent => true;

    public override bool DisableCull => true;

    public override void ApplyDepthPrepass(RenderContext rstate)
    {
        throw new InvalidOperationException();
    }
}
