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
using LibreLancer.Ini;

namespace LibreLancer.Compatibility.GameData.Universe
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
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (SunBurnthroughIntensity != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					SunBurnthroughIntensity = e[0].ToSingle();
					break;
				case "sun_burnthrough_scaler":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (SunBurnthroughScaler != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					SunBurnthroughScaler = e[0].ToSingle();
					break;
				default:
					throw new Exception("Invalid Entry in " + section.Name + ": " + e.Name);
				}
			}
		}
	}
}