using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Resources;
using LibreLancer.Shaders;
using LibreLancer.Utf.Mat;

namespace LibreLancer.Render.Materials;

public class QuadMaterial : RenderMaterial
{
    public List<(Texture texture, ushort blendMode)> Parameters = new List<(Texture texture, ushort blendMode)>();

    public int ParameterCount => Parameters.Count;

    public void ResetParameters() => Parameters.Clear();

    public int AddParameters(Texture texture, ushort blendMode)
    {
        Parameters.Add((texture, blendMode));
        return Parameters.Count - 1;
    }

    public QuadMaterial(ResourceManager library) : base(library) { }

    public override void Use(RenderContext rstate, IVertexType vertextype, ref Lighting lights, int userData)
    {
        var shader = AllShaders.Sprite.Get(0);
        Parameters[userData].texture.BindTo(0);
        rstate.BlendMode = Parameters[userData].blendMode;
        rstate.Shader = shader;
    }

    public override bool IsTransparent => true;

    public override bool DisableCull => true;
}
