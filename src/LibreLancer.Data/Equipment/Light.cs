// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;

using LibreLancer.Ini;

namespace LibreLancer.Data.Equipment
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

		private Color3f? glowColor;
		public Color3f? GlowColor
		{
			get
			{
				if (glowColor == null && Inherit != null) glowColor = Inherit.GlowColor;
				return glowColor;
			}
		}

		private Color3f? color;
		public Color3f? Color
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

		private Color3f? minColor;
		public Color3f? MinColor
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
		{
			foreach (Entry e in section)
			{
					switch (e.Name.ToLowerInvariant())
					{
                    case "nickname":
                        Nickname = e[0].ToString();
                        break;
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
							glowColor = new Color3f(e[0].ToInt32() / 255f, e[1].ToInt32() / 255f, e[2].ToInt32() / 255f);
						break;
					case "color":
						if (e.Count != 3) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
						if (color != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
							color = new Color3f(e[0].ToInt32() / 255f, e[1].ToInt32() / 255f, e[2].ToInt32() / 255f);
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
						if (minColor != null) FLLog.Warning("Light", "Duplicate " + e.Name + " Entry in " + section.Name);
							minColor = new Color3f(e[0].ToInt32() / 255f, e[1].ToInt32() / 255f, e[2].ToInt32() / 255f);
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
					case "shape":
						FLLog.Error("Light", "custom shape not implemented");
						break;
						default: FLLog.Error("Equipment", "Invalid Entry in " + section.Name + ": " + e.Name); break;
				}
			}
		}
	}
}
