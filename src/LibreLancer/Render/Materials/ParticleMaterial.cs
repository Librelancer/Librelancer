using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Shaders;
using LibreLancer.Utf.Mat;

namespace LibreLancer.Render.Materials;

public class ParticleMaterial : RenderMaterial
{
    List<(Texture texture, int Flags)> parameters = new List<(Texture texture, int Flags)>();

    public int ParameterCount => parameters.Count;

    public void ResetParameters() => parameters.Clear();

    public int AddParameters(Texture texture, ushort blendMode, bool flipU, bool flipV)
    {
        int flags = (int) blendMode;
        if (flipU) flags |= 0x40000000;
        if (flipV) flags |= 0x20000000;
        parameters.Add((texture, flags));
        return parameters.Count - 1;
    }


    public ParticleMaterial(ResourceManager library) : base(library) { }


    public override void Use(RenderContext rstate, IVertexType vertextype, ref Lighting lights, int userData)
    {
        var shader = Shaders.Particle.Get(rstate);
        shader.SetDtSampler(0);
        parameters[userData].texture.BindTo(0);
        rstate.BlendMode = (ushort)(parameters[userData].Flags & 0xFFFF);
        shader.SetFlipU((parameters[userData].Flags & 0x40000000) != 0 ? 1 : 0);
        shader.SetFlipV((parameters[userData].Flags & 0x20000000) != 0 ? 1 : 0);
        rstate.Shader = shader;
    }

    public override bool IsTransparent => true;

    public override bool DisableCull => true;

    public override void ApplyDepthPrepass(RenderContext rstate)
    {
        throw new InvalidOperationException();
    }
}
