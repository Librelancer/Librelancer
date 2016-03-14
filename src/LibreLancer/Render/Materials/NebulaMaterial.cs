using System;
using OpenTK.Graphics.OpenGL;
using LibreLancer.Vertices;
using LibreLancer.Utf.Mat;
namespace LibreLancer
{
	public class NebulaMaterial : RenderMaterial
	{
		public Texture DtSampler;
		public NebulaMaterial ()
		{
		}
		Shader GetShader(IVertexType vtype)
		{
			switch (vtype.GetType ().Name) {
			case "VertexPositionColorTexture":
				return ShaderCache.Get (
					"Basic_PositionColorTexture.vs",
					"Nebula_PositionColorTexture.frag"
				);
			default:
				throw new NotImplementedException ();
			}
		}
		public override void Use(RenderState rstate, IVertexType vertextype, Lighting lights)
		{
			//fragment shader you multiply tex sampler rgb by vertex color and alpha the same (that is should texture have alpha of its own, sometimes they may as well)
			rstate.BlendMode = BlendMode.Additive;

			var shader = GetShader(vertextype);
			shader.SetMatrix ("World", ref World);
			shader.SetMatrix ("ViewProjection", ref ViewProjection);
			//Dt
			shader.SetInteger ("DtSampler", 0);
			BindTexture (DtSampler, TextureUnit.Texture0);
			shader.UseProgram ();
		}
	}
}

