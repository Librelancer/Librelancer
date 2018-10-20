// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and confiditons defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Ini;
namespace LibreLancer.Compatibility.GameData.Solar
{
	public class Spines
	{
		public string Nickname;
		public int RadiusScale;
		public string Shape;
		public int MinRadius;
		public int MaxRadius;
		public List<Spine> Items = new List<Spine>();
		public Spines(Section s)
		{
			foreach (var e in s)
			{
				switch (e.Name.ToLowerInvariant())
				{
					case "nickname":
						Nickname = e[0].ToString();
						break;
					case "radius_scale":
						RadiusScale = e[0].ToInt32();
						break;
					case "shape":
						Shape = e[0].ToString();
						break;
					case "min_radius":
						MinRadius = e[0].ToInt32();
						break;
					case "max_radius":
						MaxRadius = e[0].ToInt32();
						break;
					case "spine":
						Items.Add(new Spine(e));
						break;
				}
			}
		}
	}
}

