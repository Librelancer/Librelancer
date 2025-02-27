using System;
using System.Numerics;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Shaders;
using LibreLancer.Utf.Mat;

namespace LibreLancer.Render.Materials;

public class SunRadialMaterial : RenderMaterial
{
    public Vector2 SizeMultiplier;
    public float OuterAlpha;
    public bool Additive;
    public string Texture;


    public SunRadialMaterial(ResourceManager library) : base(library) { }


    public override void Use(RenderContext rstate, IVertexType vertextype, ref Lighting lights, int userData)
    {
        var shader = AllShaders.SunRadial.Get(0);
        shader.SetUniformBlock(3, ref SizeMultiplier);
        shader.SetUniformBlock(4, ref OuterAlpha);
        BindTexture(rstate, 0, Texture, 0, SamplerFlags.Default);
        rstate.BlendMode = Additive ? BlendMode.Additive : BlendMode.Normal;
        rstate.Shader = shader;
    }

    public override bool IsTransparent => true;
    public override bool DisableCull => true;
}
