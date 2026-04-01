using System.Collections.Generic;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Shaders;

namespace LibreLancer.Render.Materials;


public class ParticleBeamMaterial(StorageBuffer buffer) : RenderMaterial(null)
{
    public List<(Texture texture, ushort blendMode, bool rotate, int drawStart, int drawCount)> Parameters = [];

    public StorageBuffer Buffer = buffer;


    public int AddParameters(Texture texture, ushort blendMode, bool rotate, int drawStart, int drawCount)
    {
        Parameters.Add((texture, blendMode, rotate, drawStart, drawCount));
        return Parameters.Count - 1;
    }

    public override void Use(RenderContext rstate, IVertexType vertextype, ref Lighting lights, int userData)
    {
        var shader = AllShaders.ParticleBeam.Get(0);
        Parameters[userData].texture.BindTo(0);
        int dk = Parameters[userData].rotate ? 1 : 0;
        shader.SetUniformBlock(3, ref dk);
        rstate.BlendMode = Parameters[userData].blendMode;
        rstate.Shader = shader;
        Buffer.BindTo(9, Parameters[userData].drawStart,  Parameters[userData].drawCount);
    }

    public override bool IsTransparent => true;

    public override bool DisableCull => true;
}
