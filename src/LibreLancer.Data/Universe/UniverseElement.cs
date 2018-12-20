// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Ini;

namespace LibreLancer.Data.Universe
{
	
	public abstract class UniverseElement : IniFile
	{
		protected FreelancerData GameData;

		public string Nickname { get; protected set; }
		public int IdsName { get; protected set; }
		public string Name { get; protected set; }

		public UniverseElement(FreelancerData data) {
			GameData = data;
		}

		public override string ToString()
		{
			return Nickname;
		}
	}
}
