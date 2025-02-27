using System.Runtime.InteropServices;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Shaders;
using LibreLancer.Utf.Mat;


namespace LibreLancer.Render.Materials;

public class AsteroidBandMaterial : RenderMaterial
{
    private static Shader shader;

    public Color4 ColorShift;
    public float TextureAspect;
    public string Texture;

    static void Init(RenderContext rstate)
    {
        if (shader != null) return;
        shader = AllShaders.AsteroidBand.Get(0);
    }

    public AsteroidBandMaterial(ResourceManager library) : base(library) { }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct BandParameters
    {
        public Color4 ColorShift;
        public float TextureAspect;
    }

    public override void Use(RenderContext rstate, IVertexType vertextype, ref Lighting lights, int userData)
    {
        Init(rstate);
        SetWorld(shader);
        var p = new BandParameters() { ColorShift = ColorShift, TextureAspect = TextureAspect };
        shader.SetUniformBlock(3, ref p);
        SetLights(shader, ref lights, rstate.FrameNumber);
        BindTexture(rstate, 0, Texture, 0, SamplerFlags.Default);
        rstate.BlendMode = BlendMode.Normal;
        rstate.Shader = shader;
    }

    public override bool IsTransparent => true;

    public override bool DisableCull => true;

}
