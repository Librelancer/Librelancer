// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Ini;
namespace LibreLancer.Compatibility.GameData.Solar
{
	public class LensFlare
	{
		public string Nickname;
		public string Shape;
		public int MinRadius;
		public int MaxRadius;
		public LensFlare(Section s)
		{
			foreach (var e in s)
			{
				switch (e.Name.ToLowerInvariant())
				{
					case "nickname":
						Nickname = e[0].ToString();
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
					case "bead":
						//TODO: what the hell is this (6 floats)
						break;
				}
			}
		}
	}
}

