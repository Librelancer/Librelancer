using System;
using OpenTK.Graphics.OpenGL;
namespace LibreLancer
{
	public class SunMaterial : RenderMaterial
	{
		public override void Use (RenderState rstate, LibreLancer.Vertices.IVertexType vertextype, Lighting lights)
		{
			//GL.Enable (EnableCap.DepthTest);
			var sh = ShaderCache.Get ("Sun.vs", "Sun.frag");
			sh.SetMatrix ("ViewProjection", ref ViewProjection);
			sh.SetMatrix ("World", ref World);
			sh.UseProgram ();
		}
	}
}

