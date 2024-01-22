using System;
using System.Numerics;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Shaders;
using LibreLancer.Utf.Mat;

namespace LibreLancer.Render.Materials;

public class SunSpineMaterial : RenderMaterial
{
    private static int _sizeMultiplier;
    private static ShaderVariables shader;

    public Vector2 SizeMultiplier;
    public string Texture;


    static void Init(RenderContext rstate)
    {
        if (shader != null) return;
        shader = Shaders.SunSpine.Get(rstate);
        _sizeMultiplier = shader.Shader.GetLocation("SizeMultiplier");
    }

    public SunSpineMaterial(ResourceManager library) : base(library) { }


    public override void Use(RenderContext rstate, IVertexType vertextype, ref Lighting lights, int userData)
    {
        Init(rstate);
        shader.Shader.SetVector2(_sizeMultiplier, SizeMultiplier);
        shader.SetDtSampler(0);
        BindTexture(rstate, 0, Texture, 0, SamplerFlags.Default);
        rstate.BlendMode = BlendMode.Normal;
        rstate.Shader = shader;
    }

    public override bool IsTransparent => true;
    public override bool DisableCull => true;

    public override void ApplyDepthPrepass(RenderContext rstate)
    {
        throw new InvalidOperationException();
    }
}
