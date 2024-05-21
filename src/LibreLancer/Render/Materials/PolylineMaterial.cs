using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Shaders;
using LibreLancer.Utf.Mat;

namespace LibreLancer.Render.Materials;

public class PolylineMaterial : RenderMaterial
{
    public List<(Texture texture, ushort blendMode)> Parameters = new List<(Texture texture, ushort blendMode)>();

    public PolylineMaterial(ResourceManager library) : base(library) { }

    public override void Use(RenderContext rstate, IVertexType vertextype, ref Lighting lights, int userData)
    {
        var shader = Shaders.Polyline.Get(rstate);
        shader.SetDtSampler(0);
        Parameters[userData].texture.BindTo(0);
        rstate.BlendMode = Parameters[userData].blendMode;
        rstate.Shader = shader;
    }

    public override bool IsTransparent => true;

    public override bool DisableCull => true;

    public override void ApplyDepthPrepass(RenderContext rstate)
    {
        throw new InvalidOperationException();
    }
}
