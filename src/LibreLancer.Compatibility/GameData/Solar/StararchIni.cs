// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and confiditons defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;

using LibreLancer.Ini;

namespace LibreLancer.Compatibility.GameData.Solar
{
	public class StararchIni : IniFile
	{
		public List<Star> Stars { get; private set; }
		public List<StarGlow> StarGlows { get; private set; }
		public List<LensFlare> LensFlares { get; private set; }
		public List<LensGlow> LensGlows { get; private set; }
		public List<Spines> Spines { get; private set; }
		public List<string> TextureFiles { get; private set; }
		public StararchIni(string path)
		{
			Stars = new List<Star>();
			StarGlows = new List<StarGlow>();
			LensFlares = new List<LensFlare>();
			LensGlows = new List<LensGlow>();
			Spines = new List<Spines>();
			TextureFiles = new List<string>();
			foreach (Section s in ParseFile(path))
			{
				switch (s.Name.ToLowerInvariant())
				{
					case "texture":
						foreach (var e in s)
							if (e.Name.ToLowerInvariant() == "file")
								TextureFiles.Add(e[0].ToString());
						break;
					case "star":
						Stars.Add(new Star(this, s));
						break;
					case "star_glow":
						StarGlows.Add(new StarGlow(s));
						break;
					case "lens_flare":
						LensFlares.Add(new LensFlare(s));
						break;
					case "lens_glow":
						LensGlows.Add(new LensGlow(s));
						break;
					case "spines":
						Spines.Add(new Spines(s));
						break;
					default:
						throw new Exception("Invalid Section in " + path + ": " + s.Name);
				}
			}
		}

		public Star FindStar(string nickname)
		{
			return (from Star s in Stars where s.Nickname.ToLowerInvariant() == nickname.ToLowerInvariant() select s).First<Star>();
		}

		public StarGlow FindStarGlow(string nickname)
		{
			return (from StarGlow s in StarGlows where s.Nickname.ToLowerInvariant() == nickname.ToLowerInvariant() select s).First();
		}

		public Spines FindSpines(string nickname)
		{
			return (from Spines s in Spines where s.Nickname.ToLowerInvariant() == nickname.ToLowerInvariant() select s).FirstOrDefault();
		}
	}
}