// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Ini;

namespace LibreLancer.Data.Universe
{
	public class Field
	{
		public int? CubeSize { get; private set; }
		public int? FillDist { get; private set; }
		public Color4? TintField { get; private set; }
		public float? MaxAlpha { get; private set; }
		public Color4? DiffuseColor { get; private set; }
		public Color4? AmbientColor { get; private set; }
		public Color4? AmbientIncrease { get; private set; }
		public float? EmptyCubeFrequency { get; private set; }
		public bool? ContainsFogZone { get; private set; }

		public Field(Section section)
		{
			if (section == null) throw new ArgumentNullException("section");

			foreach (Entry e in section)
			{
				switch (e.Name.ToLowerInvariant())
				{
				case "cube_size":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (CubeSize != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					CubeSize = e[0].ToInt32();
					break;
				case "fill_dist":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (FillDist != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					FillDist = e[0].ToInt32();
					break;
				case "tint_field":
					if (e.Count != 3) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (TintField != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					TintField = new Color4(e[0].ToInt32() / 255f, e[1].ToInt32() / 255f, e[2].ToInt32() / 255f, 1f);
					break;
				case "max_alpha":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (MaxAlpha != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					MaxAlpha = e[0].ToSingle();
					break;
				case "diffuse_color":
					if (e.Count != 3) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (DiffuseColor != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					DiffuseColor = new Color4(e[0].ToInt32() / 255f, e[1].ToInt32() / 255f, e[2].ToInt32() / 255f, 1f);
					break;
				case "ambient_color":
					if (e.Count != 3) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (AmbientColor != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					AmbientColor = new Color4(e[0].ToInt32() / 255f, e[1].ToInt32() / 255f, e[2].ToInt32() / 255f, 1f);
					break;
				case "ambient_increase":
					if (e.Count != 3) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (AmbientIncrease != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					AmbientIncrease = new Color4(e[0].ToInt32() / 255f, e[1].ToInt32() / 255f, e[2].ToInt32() / 255f, 1f);
					break;
				case "empty_cube_frequency":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (EmptyCubeFrequency != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					EmptyCubeFrequency = e[0].ToSingle();
					break;
				case "contains_fog_zone":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (ContainsFogZone != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					//ContainsFogZone = bool.Parse(e[0].ToString());
					ContainsFogZone = e[0].ToBoolean();
					break;
				default:
					FLLog.Error("Ini", "Invalid Entry in " + section.Name + ": " + e.Name);
					break;
				}
			}
		}
	}
}