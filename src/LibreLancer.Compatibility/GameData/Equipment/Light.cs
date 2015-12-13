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
 * Portions created by the Initial Developer are Copyright (C) 2011, 2012
 * the Initial Developer. All Rights Reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;

using OpenTK;
using OpenTK.Graphics;

using LibreLancer.Ini;

namespace LibreLancer.Compatibility.GameData.Equipment
{
	public class Light : AbstractEquipment
	{
		public Light Inherit { get; private set; }

		private bool? alwaysOn;
		public bool? AlwaysOn
		{
			get
			{
				if (alwaysOn == null && Inherit != null) alwaysOn = Inherit.AlwaysOn;
				return alwaysOn;
			}
		}

		private bool? dockingLight;
		public bool? DockingLight
		{
			get
			{
				if (dockingLight == null && Inherit != null) dockingLight = Inherit.DockingLight;
				return dockingLight;
			}
		}

		private float? bulbSize;
		public float? BulbSize
		{
			get
			{
				if (bulbSize == null && Inherit != null) bulbSize = Inherit.BulbSize;
				return bulbSize;
			}
		}

		private float? glowSize;
		public float? GlowSize
		{
			get
			{
				if (glowSize == null && Inherit != null) glowSize = Inherit.GlowSize;
				return glowSize;
			}
		}

		private Color4? glowColor;
		public Color4? GlowColor
		{
			get
			{
				if (glowColor == null && Inherit != null) glowColor = Inherit.GlowColor;
				return glowColor;
			}
		}

		private Color4? color;
		public Color4? Color
		{
			get
			{
				if (color == null && Inherit != null) color = Inherit.Color;
				return color;
			}
		}

		private Vector2? flareCone;
		public Vector2? FlareCone
		{
			get
			{
				if (flareCone == null && Inherit != null) flareCone = Inherit.FlareCone;
				return flareCone;
			}
		}

		private int? intensity;
		public int? Intensity
		{
			get
			{
				if (intensity == null && Inherit != null) intensity = Inherit.Intensity;
				return intensity;
			}
		}

		private int? lightsourceCone;
		public int? LightsourceCone
		{
			get
			{
				if (lightsourceCone == null && Inherit != null) lightsourceCone = Inherit.LightsourceCone;
				return lightsourceCone;
			}
		}

		private Color4? minColor;
		public Color4? MinColor
		{
			get
			{
				if (minColor == null && Inherit != null) minColor = Inherit.MinColor;
				return minColor;
			}
		}

		private float? avgDelay;
		public float? AvgDelay
		{
			get
			{
				if (avgDelay == null && Inherit != null) avgDelay = Inherit.AvgDelay;
				return avgDelay;
			}
		}

		private float? blinkDuration;
		public float? BlinkDuration
		{
			get
			{
				if (blinkDuration == null && Inherit != null) blinkDuration = Inherit.BlinkDuration;
				return blinkDuration;
			}
		}

		public Light(Section section, FreelancerData gdata)
			: base(section)
		{
			foreach (Entry e in section)
			{
				if (!parentEntry(e))
				{
					switch (e.Name.ToLowerInvariant())
					{
					case "inherit":
						if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (Inherit != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						Inherit = gdata.Equipment.FindEquipment(e[0].ToString()) as Light;
						break;
					case "always_on":
						if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (alwaysOn != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						alwaysOn = e[0].ToBoolean();
						break;
					case "docking_light":
						if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (dockingLight != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						dockingLight = e[0].ToBoolean();
						break;
					case "bulb_size":
						if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (bulbSize != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						bulbSize = e[0].ToSingle();
						break;
					case "glow_size":
						if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (glowSize != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						glowSize = e[0].ToSingle();
						break;
					case "glow_color":
						if (e.Count != 3) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (glowColor != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						glowColor = new Color4(e[0].ToInt32() / 255f, e[1].ToInt32() / 255f, e[2].ToInt32() / 255f, 1f);
						break;
					case "color":
						if (e.Count != 3) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (color != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						color = new Color4(e[0].ToInt32() / 255f , e[1].ToInt32() / 255f, e[2].ToInt32() / 255f, 1f);
						break;
					case "flare_cone":
						if (e.Count != 2) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (flareCone != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						flareCone = new Vector2(e[0].ToInt32(), e[1].ToInt32());
						break;
					case "intensity":
						if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (intensity != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						intensity = e[0].ToInt32();
						break;
					case "lightsource_cone":
						if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (lightsourceCone != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						lightsourceCone = e[0].ToInt32();
						break;
					case "min_color":
						if (e.Count != 3) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (minColor != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						minColor = new Color4(e[0].ToInt32() / 255f, e[1].ToInt32() / 255f, e[2].ToInt32() / 255f, 1f);
						break;
					case "avg_delay":
						if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (avgDelay != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						avgDelay = e[0].ToSingle();
						break;
					case "blink_duration":
						if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (blinkDuration != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
						blinkDuration = e[0].ToSingle();
						break;
					default: throw new Exception("Invalid Entry in " + section.Name + ": " + e.Name);
					}
				}
			}
		}
	}
}
