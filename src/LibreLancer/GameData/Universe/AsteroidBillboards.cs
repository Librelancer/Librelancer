/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * The Original Code is Starchart code (http://flapi.sourceforge.net/).
 * 
 * The Initial Developer of the Original Code is Malte Rupprecht (mailto:rupprema@googlemail.com).
 * Portions created by the Initial Developer are Copyright (C) 2011
 * the Initial Developer. All Rights Reserved.
 */

using System;
using System.Linq;

using OpenTK;

using LibreLancer.Ini;

namespace LibreLancer.GameData.Universe
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