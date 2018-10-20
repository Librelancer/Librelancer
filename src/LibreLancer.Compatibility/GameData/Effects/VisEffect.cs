// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and confiditons defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Ini;
namespace LibreLancer.Compatibility.GameData.Effects
{
	public class VisEffect
	{
		public string Nickname;
		public int EffectCrc;
		public string AlchemyPath;
		public List<string> Textures = new List<string>();
		public VisEffect(Section s)
		{
			foreach (var e in s)
			{
				switch (e.Name.ToLowerInvariant())
				{
					case "nickname":
						Nickname = e[0].ToString();
						break;
					case "alchemy":
						AlchemyPath = e[0].ToString();
						break;
					case "textures":
						Textures.Add(e[0].ToString());
						break;
					case "effect_crc":
						EffectCrc = e[0].ToInt32();
						break;
						
				}
			}
		}
	}
}
