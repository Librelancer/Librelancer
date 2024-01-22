using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Shaders;
using LibreLancer.Utf.Mat;


namespace LibreLancer.Render.Materials;

public class AsteroidBandMaterial : RenderMaterial
{
    private static ShaderVariables shader;
    private static int _textureAspect;
    private static int _colorShift;

    public Color4 ColorShift;
    public float TextureAspect;
    public string Texture;

    static void Init(RenderContext rstate)
    {
        if (shader != null) return;
        shader = AsteroidBand.Get(rstate);
        _colorShift = shader.Shader.GetLocation("ColorShift");
        _textureAspect = shader.Shader.GetLocation("TextureAspect");
    }

    public AsteroidBandMaterial(ResourceManager library) : base(library) { }

    public override void Use(RenderContext rstate, IVertexType vertextype, ref Lighting lights, int userData)
    {
        Init(rstate);
        shader.Shader.SetColor4(_colorShift, ColorShift);
        shader.Shader.SetFloat(_textureAspect, TextureAspect);
        shader.SetWorld(World);
        shader.SetDtSampler(0);
        SetLights(shader, ref lights, rstate.FrameNumber);
        BindTexture(rstate, 0, Texture, 0, SamplerFlags.Default);
        rstate.BlendMode = BlendMode.Normal;
        rstate.Shader = shader;
    }

    public override bool IsTransparent => true;

    public override bool DisableCull => true;

    public override void ApplyDepthPrepass(RenderContext rstate)
    {
        throw new System.InvalidOperationException();
    }
}
