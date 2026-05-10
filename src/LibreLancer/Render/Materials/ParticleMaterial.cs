using System.Collections.Generic;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Shaders;

namespace LibreLancer.Render.Materials;

public enum ParticleDrawKind
{
    Basic,
    Rect,
    Perp
}

public class ParticleMaterial(StorageBuffer buffer) : RenderMaterial(null)
{
    public List<(Texture texture, ushort blendMode, ParticleDrawKind drawKind, int drawStart, int drawCount)> Parameters = [];

    public StorageBuffer Buffer = buffer;


    public int AddParameters(Texture texture, ushort blendMode, ParticleDrawKind drawKind, int drawStart, int drawCount)
    {
        Parameters.Add((texture, blendMode, drawKind, drawStart, drawCount));
        return Parameters.Count - 1;
    }

    public override void Use(RenderContext rstate, IVertexType vertextype, ref Lighting lights, int userData)
    {
        var shader = AllShaders.Particle.Get(0);
        rstate.Textures[0] = Parameters[userData].texture;
        rstate.Samplers[0] = new SamplerState(rstate.PreferredFilterLevel, WrapMode.ClampToEdge, WrapMode.ClampToEdge);
        var dk = Parameters[userData].drawKind;
        shader.SetUniformBlock(3, ref dk);
        rstate.BlendMode = Parameters[userData].blendMode;
        rstate.Shader = shader;
        Buffer.BindTo(9, Parameters[userData].drawStart,  Parameters[userData].drawCount);
    }

    public override bool IsTransparent => true;

    public override bool DisableCull => true;
}
