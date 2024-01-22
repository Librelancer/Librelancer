using System;
using System.Numerics;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Shaders;
using LibreLancer.Utf.Mat;

namespace LibreLancer.Render.Materials;

public class SunRadialMaterial : RenderMaterial
{
    private static int _sizeMultiplier;
    private static int _outerAlpha;
    private static ShaderVariables shader;

    public Vector2 SizeMultiplier;
    public float OuterAlpha;
    public bool Additive;
    public string Texture;


    static void Init(RenderContext rstate)
    {
        if (shader != null) return;
        shader = Shaders.SunRadial.Get(rstate);
        _sizeMultiplier = shader.Shader.GetLocation("SizeMultiplier");
        _outerAlpha = shader.Shader.GetLocation("outerAlpha");
    }

    public SunRadialMaterial(ResourceManager library) : base(library) { }


    public override void Use(RenderContext rstate, IVertexType vertextype, ref Lighting lights, int userData)
    {
        Init(rstate);
        shader.Shader.SetVector2(_sizeMultiplier, SizeMultiplier);
        shader.Shader.SetFloat(_outerAlpha, OuterAlpha);
        shader.SetDtSampler(0);
        BindTexture(rstate, 0, Texture, 0, SamplerFlags.Default);
        rstate.BlendMode = Additive ? BlendMode.Additive : BlendMode.Normal;
        rstate.Shader = shader;
    }

    public override bool IsTransparent => true;
    public override bool DisableCull => true;

    public override void ApplyDepthPrepass(RenderContext rstate)
    {
        throw new InvalidOperationException();
    }
}
