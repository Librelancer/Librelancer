// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and confiditons defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Ini;
namespace LibreLancer.Compatibility.GameData.Effects
{
	public class Effect
	{
		public string Nickname;
		public string VisEffect;
		public Effect(Section s)
		{
			foreach (var e in s)
			{
				switch (e.Name.ToLowerInvariant())
				{
					case "nickname":
						Nickname = e[0].ToString();
						break;
					case "vis_effect":
						if(e.Count > 0) VisEffect = e[0].ToString();
						break;
				}
			}
		}
	}
}
