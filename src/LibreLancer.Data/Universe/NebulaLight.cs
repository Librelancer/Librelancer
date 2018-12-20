// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Ini;

namespace LibreLancer.Data.Universe
{
	public class NebulaLight
	{
		public Color4? Ambient { get; private set; }
		public float? SunBurnthroughIntensity { get; private set; }
		public float? SunBurnthroughScaler { get; private set; }

		public NebulaLight(Section section)
		{
			if (section == null) throw new ArgumentNullException("section");

			foreach (Entry e in section)
			{
				switch (e.Name.ToLowerInvariant())
				{
				case "ambient":
					if (e.Count != 3) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (Ambient != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					Ambient = new Color4(e[0].ToInt32() / 255f, e[1].ToInt32() / 255f, e[2].ToInt32() / 255f, 1f);
					break;
				case "sun_burnthrough_intensity":
					if (e.Count != 1) FLLog.Warning("Ini", "Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (SunBurnthroughIntensity != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					SunBurnthroughIntensity = e[0].ToSingle();
					break;
				case "sun_burnthrough_scaler":
					if (e.Count != 1)  FLLog.Warning("Ini", "Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (SunBurnthroughScaler != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					SunBurnthroughScaler = e[0].ToSingle();
					break;
				default:
					FLLog.Warning ("Ini", "Invalid Entry in " + section.Name + ": " + e.Name);
					break;
				}
			}
		}
	}
}