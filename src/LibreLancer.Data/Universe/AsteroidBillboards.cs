// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;

using LibreLancer.Ini;

namespace LibreLancer.Data.Universe
{
	public class AsteroidBillboards
	{
		public int? Count { get; private set; }

		public int? StartDist { get; private set; }

		public float? FadeDistPercent { get; private set; }

		public string Shape { get; private set; }

		public Vector3? ColorShift { get; private set; }

		public float? AmbientIntensity { get; private set; }

		public Vector2? Size { get; private set; }

		public AsteroidBillboards (Section section)
		{
			if (section == null)
				throw new ArgumentNullException ("section");

			foreach (Entry e in section) {
				switch (e.Name.ToLowerInvariant ()) {
				case "count":
					if (e.Count != 1)
						throw new Exception ("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (Count != null)
						throw new Exception ("Duplicate " + e.Name + " Entry in " + section.Name);
					Count = e [0].ToInt32 ();
					break;
				case "start_dist":
					if (e.Count != 1)
						throw new Exception ("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (StartDist != null)
						throw new Exception ("Duplicate " + e.Name + " Entry in " + section.Name);
					StartDist = e [0].ToInt32 ();
					break;
				case "fade_dist_percent":
					if (e.Count != 1)
						throw new Exception ("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (FadeDistPercent != null)
						throw new Exception ("Duplicate " + e.Name + " Entry in " + section.Name);
					FadeDistPercent = e [0].ToSingle ();
					break;
				case "shape":
					if (e.Count != 1)
						throw new Exception ("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (Shape != null)
						throw new Exception ("Duplicate " + e.Name + " Entry in " + section.Name);
					Shape = e [0].ToString ();
					break;
				case "color_shift":
					if (e.Count != 3)
						throw new Exception ("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (ColorShift != null)
						throw new Exception ("Duplicate " + e.Name + " Entry in " + section.Name);
					ColorShift = new Vector3 (e [0].ToSingle (), e [1].ToSingle (), e [2].ToSingle ());
					break;
				case "ambient_intensity":
					if (e.Count != 1)
						throw new Exception ("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (AmbientIntensity != null)
						throw new Exception ("Duplicate " + e.Name + " Entry in " + section.Name);
					AmbientIntensity = e [0].ToSingle ();
					break;
				case "size":
					if (e.Count != 2)
						throw new Exception ("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (Size != null)
						throw new Exception ("Duplicate " + e.Name + " Entry in " + section.Name);
					Size = new Vector2 (e [0].ToInt32 (), e [1].ToInt32 ());
					break;
				default:
					throw new Exception ("Invalid Entry in " + section.Name + ": " + e.Name);
				}
			}
		}
	}
}