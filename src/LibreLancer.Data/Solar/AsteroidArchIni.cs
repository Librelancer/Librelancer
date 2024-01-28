// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.IO;
using LibreLancer.Ini;
namespace LibreLancer.Data.Solar
{
	public class AsteroidArchIni : IniFile
	{
		public List<Asteroid> Asteroids = new List<Asteroid> ();
		public void AddFile (string path, FileSystem vfs)
		{
			foreach (var s in ParseFile(path, vfs)) {
				switch (s.Name.ToLowerInvariant ()) {
				case "asteroid":
				case "asteroidmine":
					Asteroids.Add (new Asteroid (s));
					break;
				}
			}
		}
		public Asteroid FindAsteroid(string nickname)
		{
			IEnumerable<Asteroid> candidates = from Asteroid s in Asteroids where s.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase) select s;
			int count = candidates.Count<Asteroid>();
			if (count == 1) return candidates.First<Asteroid>();
			else if (count == 0) return null;
			else throw new Exception(count + " Asteroids with nickname " + nickname);
		}
	}
}

