// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.IO;
using LibreLancer.Ini;

namespace LibreLancer.Data.Solar
{
	public class StararchIni : IniFile
	{
        public List<Star> Stars = new List<Star>();
        public List<StarGlow> StarGlows = new List<StarGlow>();
        public List<LensFlare> LensFlares = new List<LensFlare>();
        public List<LensGlow> LensGlows = new List<LensGlow>();
        public List<Spines> Spines = new List<Spines>();
        public List<string> TextureFiles = new List<string>();

		public StararchIni(string path, FileSystem vfs)
		{
			foreach (Section s in ParseFile(path, vfs))
			{
				switch (s.Name.ToLowerInvariant())
				{
					case "texture":
						foreach (var e in s)
							if (e.Name.ToLowerInvariant() == "file")
								TextureFiles.Add(e[0].ToString());
						break;
					case "star":
                        Stars.Add(FromSection<Star>(s));
						break;
					case "star_glow":
						StarGlows.Add(FromSection<StarGlow>(s));
						break;
					case "lens_flare":
                        LensFlares.Add(FromSection<LensFlare>(s));
						break;
					case "lens_glow":
						LensGlows.Add(FromSection<LensGlow>(s));
						break;
					case "spines":
						Spines.Add(FromSection<Spines>(s));
						break;
					default:
						FLLog.Warning("Ini", "Invalid Section in " + path + ": " + s.Name + ", " + s.Line);
                        break;
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