using System;
using LibreLancer.Ini;
using OpenTK;

namespace LibreLancer.GameData.Universe
{
	public class Asteroid
	{
		public string Name { get; private set; }
		public Vector3 Rotation { get; private set; }
		public Vector3 Size { get; private set; }
		public string Info { get; private set; }

		public Asteroid (Entry e)
		{
			Name = e[0].ToString();
			Rotation =  new Vector3(e[1].ToSingle(), e[2].ToSingle(), e[3].ToSingle());
			Size = new Vector3(e[4].ToSingle(), e[5].ToSingle(), e[6].ToSingle());
			Info = e.Count == 8 ? e[7].ToString() : string.Empty;
		}
	}
}

