using System;
using OpenTK.Graphics.OpenGL;
namespace LibreLancer
{
	static class GLBind
	{
		static int programBound = -1;
		public static void UseProgram(int prg)
		{
			if (programBound != prg) {
				GL.UseProgram (prg);
				programBound = prg;
			}
		}

		static int bound_vbo = -1;
		static int bound_vao = -1;

		public static void VertexBuffer(int vbo)
		{
			if (bound_vbo != vbo) {
				bound_vbo = vbo;
				GL.BindBuffer (BufferTarget.ArrayBuffer, vbo);
			}
		}
		public static void VertexArray(int vao)
		{
			if (bound_vao != vao) {
				bound_vao = vao;
				GL.BindVertexArray (vao);
			}
		}
	}
}

