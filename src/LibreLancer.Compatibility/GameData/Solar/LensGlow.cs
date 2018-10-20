// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Ini;
namespace LibreLancer.Compatibility.GameData.Solar
{
	public class LensGlow
	{
		public string Nickname;
		public string Shape;
		public int RadiusScale;
		public Color3f InnerColor;
		public Color3f OuterColor;
		public float GlowFadeInSeconds;
		public float GlowFadeOutSeconds;

		public LensGlow(Section s)
		{
			foreach (Entry e in s)
			{
				switch (e.Name.ToLowerInvariant())
				{
					case "nickname":
						Nickname = e[0].ToString();
						break;
					case "shape":
						Shape = e[0].ToString();
						break;
					case "radius_scale":
						RadiusScale = e[0].ToInt32();
						break;
					case "inner_color":
						InnerColor = new Color3f(e[0].ToSingle(), e[1].ToSingle(), e[2].ToSingle());
						break;
					case "outer_color":
						OuterColor = new Color3f(e[0].ToSingle(), e[1].ToSingle(), e[2].ToSingle());
						break;
					case "glow_fade_in_seconds":
						GlowFadeInSeconds = e[0].ToSingle();
						break;
					case "glow_fade_out_seconds":
						GlowFadeOutSeconds = e[0].ToSingle();
						break;
				}
			}
		}
	}
}

