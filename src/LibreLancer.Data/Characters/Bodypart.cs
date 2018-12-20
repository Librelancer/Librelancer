// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

using LibreLancer.Ini;

namespace LibreLancer.Data.Characters
{
	public class Bodypart
	{
		public string Nickname { get; private set; }
		FreelancerData gameData;
		public string MeshPath = null;

		public Bodypart(Section s, FreelancerData gdata)
		{
			gameData = gdata;
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
					MeshPath = VFS.GetPath (gdata.Freelancer.DataPath + e [0].ToString ());
					break;
				default: throw new Exception("Invalid Entry in " + s.Name + ": " + e.Name);
				}
			}
		}
	}
}
