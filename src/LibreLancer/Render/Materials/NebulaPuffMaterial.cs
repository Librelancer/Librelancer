using System;
using System.Numerics;
using LibreLancer.Shaders;
using LibreLancer.Utf.Mat;
using LibreLancer.Vertices;

namespace LibreLancer.Render.Materials;

public class NebulaPuffMaterial : RenderMaterial
{
    private static ShaderVariables shader;
    private static int _fogFactor;

    public string Texture;

    static NebulaPuffMaterial()
    {
        shader = Shaders.NebulaExtPuff.Get();
        _fogFactor = shader.Shader.GetLocation("FogFactor");
    }

    public NebulaPuffMaterial(ResourceManager library) : base(library) { }


    public override unsafe void Use(RenderContext rstate, IVertexType vertextype, ref Lighting lights, int userData)
    {
        shader.SetWorld(World);
        shader.SetDtSampler(0);
        shader.Shader.SetFloat(_fogFactor, *(float*)&userData);
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
