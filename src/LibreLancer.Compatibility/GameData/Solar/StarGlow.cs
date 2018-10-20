// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Ini;
namespace LibreLancer.Compatibility.GameData.Solar
{
	public class StarGlow
	{
		public string Nickname;
		public string Shape;
		public int Scale;
		public Color3f InnerColor;
		public Color3f OuterColor;
		public StarGlow(Section section)
		{
			foreach (var e in section)
			{
				switch (e.Name.ToLowerInvariant())
				{
					case "nickname":
						Nickname = e[0].ToString();
						break;
					case "shape":
						Shape = e[0].ToString();
						break;
					case "scale":
						Scale = e[0].ToInt32();
						break;
					case "inner_color":
						InnerColor = new Color3f(e[0].ToSingle(), e[1].ToSingle(), e[2].ToSingle());
						break;
					case "outer_color":
						OuterColor = new Color3f(e[0].ToSingle(), e[1].ToSingle(), e[2].ToSingle());
						break;
				}
			}
		}
	}
}

