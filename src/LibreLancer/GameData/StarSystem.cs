using System;
using System.Collections.Generic;
using OpenTK.Graphics;
namespace LibreLancer.GameData
{
	public class StarSystem
	{
		public string Id;
		public string Name;
		//Background
		public Color4 BackgroundColor;
		//Starsphere
		public IDrawable StarsBasic;
		public IDrawable StarsComplex;
		public IDrawable StarsNebula;
		//Lighting
		public Color4 AmbientColor;
		public List<RenderLight> LightSources = new List<RenderLight>();
		//Objects
		public List<SystemObject> Objects = new List<SystemObject>();
		//Nebulae
		public List<Nebula> Nebulae = new List<Nebula>();
		//Zones
		public List<Zone> Zones = new List<Zone>();

		public StarSystem ()
		{
		}
	}
}

