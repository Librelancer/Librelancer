using System;
namespace LibreLancer
{
	public struct RenderUserData
	{
		public Matrix4 ViewProjection;
		public Color4 Color;
		public Color4 Color2;
		public Texture Texture;
		public float Float;
		public float Float2;
		public Action<Shader, RenderUserData> UserFunction;

	}
}

