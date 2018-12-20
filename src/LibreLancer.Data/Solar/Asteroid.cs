// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Ini;
namespace LibreLancer.Data.Solar
{
	public class Asteroid
	{
		public string Nickname;
		public string DaArchetype;
		public string MaterialLibrary;
		public bool IsMine;
		public Asteroid (Section section)
		{
			IsMine = section.Name.ToLowerInvariant () == "asteroidmine";
			foreach (var e in section) {
				switch (e.Name.ToLowerInvariant ()) {
				case "nickname":
					Nickname = e [0].ToString ();
					break;
				case "da_archetype":
					DaArchetype = e [0].ToString ();
					break;
				case "material_library":
					MaterialLibrary = e [0].ToString ();
					break;
				}
			}
		}
	}
}

