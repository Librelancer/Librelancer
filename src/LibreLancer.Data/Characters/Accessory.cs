// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

using LibreLancer.Ini;

namespace LibreLancer.Data.Characters
{
	public class Accessory
	{
		public string Nickname { get; private set; }
		FreelancerData GameData;
		public string MeshPath = null;

		public Accessory(Section s, FreelancerData gdata)
		{
			GameData = gdata;
			foreach (Entry e in s)
			{
				switch (e.Name.ToLowerInvariant())
				{
				case "nickname":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
					if (Nickname != null) throw new Exception("Duplicate " + e.Name + " Entry in " + s.Name);
					Nickname = e[0].ToString();
					break;
				case "mesh":
					if (e.Count != 1)
						throw new Exception ("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
					if (MeshPath != null)
						throw new Exception ("Duplicate " + e.Name + " Entry in " + s.Name);
					MeshPath = VFS.GetPath (GameData.Freelancer.DataPath + e [0].ToString ());
					break;
				case "hardpoint":
					// TODO: Accessory hardpoint
					break;
				case "body_hardpoint":
					// TODO: Accessory body_hardpoint
					break;
				default: throw new Exception("Invalid Entry in " + s.Name + ": " + e.Name);
				}
			}
		}
	}
}
