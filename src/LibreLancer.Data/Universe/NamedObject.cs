// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using LibreLancer.Ini;

namespace LibreLancer.Data.Universe
{
	public abstract class NamedObject
	{
		private Section section;
		protected FreelancerData GameData;

		public string Nickname { get; private set; }
		public Vector3? Pos { get; private set; }
		public Vector3? Rotate { get; private set; }

		protected NamedObject(Section section, FreelancerData data)
		{
			if (section == null) throw new ArgumentNullException("section");

			this.section = section;
			GameData = data;
		}

		protected NamedObject(Vector3 pos, FreelancerData data)
		{
			Pos = pos;
			GameData = data;
		}

		protected virtual bool parentEntry(Entry e)
		{
			switch (e.Name.ToLowerInvariant())
			{
			case "nickname":
				//if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
				if (e.Count != 1) FLLog.Warning("Ini", "Object " + e[0].ToString() + " has multiple values in nickname section");
				if (Nickname != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
				Nickname = e[0].ToString();
				break;
			case "pos":
				if (e.Count != 3) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
				if (Pos != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
				Pos = new Vector3(e[0].ToSingle(), e[1].ToSingle(), e[2].ToSingle());
				break;
			case "rotate":
				if (e.Count == 1) {
					if (e [0].ToSingle () == 0)
						Rotate = Vector3.Zero;
					else
						FLLog.Warning ("INI", "Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
				} else {
                        if (e.Count != 3)
                        {
                            FLLog.Error("Universe", "Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
                            break;
                        }
					if (Rotate != null)
						throw new Exception ("Duplicate " + e.Name + " Entry in " + section.Name);
					Rotate = new Vector3 (e [0].ToSingle (), e [1].ToSingle (), e [2].ToSingle ());
				}
				break;
			default: return false;
			}

			return true;
		}

		public override string ToString()
		{
			return Nickname;
		}
	}
}
