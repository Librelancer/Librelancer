using System;
using System.Collections.Generic;
using OpenTK.Graphics;
namespace LibreLancer
{
	public class Lighting
	{
		public Color4 Ambient = Color4.White;
		public List<RenderLight> Lights = new List<RenderLight>();
		public Lighting ()
		{
		}
	}
}

