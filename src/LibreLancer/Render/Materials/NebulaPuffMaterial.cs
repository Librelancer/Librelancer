using System;
using System.Numerics;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Resources;
using LibreLancer.Shaders;
using LibreLancer.Utf.Mat;

namespace LibreLancer.Render.Materials;

public class NebulaPuffMaterial : RenderMaterial
{
    public string Texture;

    public NebulaPuffMaterial(ResourceManager library) : base(library) { }


    public override unsafe void Use(RenderContext rstate, IVertexType vertextype, ref Lighting lights, int userData)
    {
        var shader = AllShaders.NebulaExtPuff.Get(0);
        SetWorld(shader);
        shader.SetUniformBlock(3, ref userData);
        BindTexture(rstate, 0, Texture, 0, SamplerFlags.Default);
        rstate.BlendMode = BlendMode.Normal;
        rstate.Shader = shader;
    }

    public override bool IsTransparent => true;
    public override bool DisableCull => true;
}
