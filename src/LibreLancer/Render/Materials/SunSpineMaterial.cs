using System;
using System.Numerics;
using LibreLancer.Shaders;
using LibreLancer.Utf.Mat;
using LibreLancer.Vertices;

namespace LibreLancer.Render.Materials;

public class SunSpineMaterial : RenderMaterial
{
    private static int _sizeMultiplier;
    private static ShaderVariables shader;

    public Vector2 SizeMultiplier;
    public string Texture;


    static SunSpineMaterial()
    {
        shader = Shaders.SunSpine.Get();
        _sizeMultiplier = shader.Shader.GetLocation("SizeMultiplier");
    }

    public SunSpineMaterial(ResourceManager library) : base(library) { }


    public override void Use(RenderContext rstate, IVertexType vertextype, ref Lighting lights, int userData)
    {
        shader.Shader.SetVector2(_sizeMultiplier, SizeMultiplier);
        shader.SetDtSampler(0);
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
