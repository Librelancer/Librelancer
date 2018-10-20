// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
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
		//Asteroid Fields
		public List<AsteroidField> AsteroidFields = new List<AsteroidField>();
		//Zones
		public List<Zone> Zones = new List<Zone>();
		//Music
		public string MusicSpace;
		//Clipping
		public float FarClip;
		public StarSystem ()
		{
		}
	}
}

