using System;
using OpenTK;
using OpenTK.Graphics;
namespace LibreLancer
{
	public class RenderLight
	{
		public Vector3 Position; 
		public Vector3 Rotation;
		public Color4 Color;
		public int Range;
		public Vector3 Attenuation;

		public RenderLight ()
		{
		}
	}
}

