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
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using LibreLancer.Ini;

namespace LibreLancer.GameData.Universe
{
	public class LightSource : NamedObject
	{
		public Color4? Color { get; private set; } // = 255, 255, 255
		public int? Range { get; private set; } // = 120000
		public LightType? Type { get; private set; }
		public string AttenCurve { get; private set; } // = DYNAMIC_DIRECTION
		public Vector3? Attenuation { get; private set; }
		public Vector3? Direction { get; private set; } // = 642, 0, 198

		public LightSource(Vector3 pos, Color4 color, int range, FreelancerData data)
			: base(pos, data)
		{
			Color = color;
			Range = range;
		}

		public LightSource(Section section, FreelancerData data)
			: base(section, data)
		{
			if (section == null) throw new ArgumentNullException("section");

			foreach (Entry e in section)
			{
				if (!parentEntry(e))
				{
					switch (e.Name.ToLowerInvariant())
					{
					case "ids_name": // ignore
						break;
					case "color":
						if (e.Count != 3) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (Color != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						Color = new Color4(e[0].ToInt32() / 255f, e[1].ToInt32() / 255f, e[2].ToInt32() / 255f, 1f);
						break;
					case "range":
						if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (Range != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						Range = e[0].ToInt32();
						break;
					case "type":
						if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (Type != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						switch (e[0].ToString().ToUpperInvariant())
						{
						case "DIRECTIONAL": Type = LightType.Directional; break;
						case "POINT": Type = LightType.Point; break;
						default: throw new Exception("Invalid Value in " + e.Name + ": " + e[0].ToString());
						}
						break;
					case "atten_curve":
						if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (AttenCurve != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						AttenCurve = e[0].ToString();
						break;
					case "attenuation":
						if (e.Count != 3) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (Attenuation != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						Attenuation = new Vector3(e[0].ToSingle(), e[1].ToSingle(), e[2].ToSingle());
						break;
					case "direction":
						if (e.Count != 3) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (Direction != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						Direction = new Vector3(e[0].ToSingle(), e[1].ToSingle(), e[2].ToSingle());
						break;
					case "behavior": // ignore
						break;
					case "color_curve": // string, float
						break;
					default:
						throw new Exception("Invalid Entry in " + section.Name + ": " + e.Name);
					}
				}
			}
		}
	}
}
