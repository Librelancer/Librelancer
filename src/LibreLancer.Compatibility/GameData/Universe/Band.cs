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
using System.Linq;
using LibreLancer.Ini;

namespace LibreLancer.Compatibility.GameData.Universe
{
	public class Band
	{
		protected StarSystem parent;

		private string zoneName;
		private Zone zone;
		public Zone Zone
		{
			get
			{
				if (zone == null) zone = parent.FindZone(zoneName);
				return zone;
			}
		}

		public int? RenderParts { get; private set; }
		public string Shape { get; private set; }
		public int? Height { get; private set; }
		public int? OffsetDist { get; private set; }
		public List<float> Fade { get; private set; }
		public float? TextureAspect { get; private set; }
		public Vector3? ColorShift { get; private set; }
		public float? AmbientIntensity { get; private set; }
		public int? CullMode { get; private set; }
		public int? VertIncrease { get; private set; }

		public Band(StarSystem parent, Section section)
		{
			if (parent == null) throw new ArgumentNullException("parent");
			if (section == null) throw new ArgumentNullException("section");

			this.parent = parent;

			foreach (Entry e in section)
			{
				switch (e.Name.ToLowerInvariant())
				{
				case "zone":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (zoneName != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					zoneName = e[0].ToString();
					break;
				case "render_parts":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (RenderParts != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					RenderParts = e[0].ToInt32();
					break;
				case "shape":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (Shape != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					Shape = e[0].ToString();
					break;
				case "height":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (Height != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					Height = e[0].ToInt32();
					break;
				case "offset_dist":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (OffsetDist != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					OffsetDist = e[0].ToInt32();
					break;
				case "fade":
					if (e.Count != 4) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (Fade != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					Fade = new List<float>();
					foreach (IValue i in e) Fade.Add(e[0].ToSingle());
					break;
				case "texture_aspect":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (TextureAspect != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					TextureAspect = e[0].ToSingle();
					break;
				case "color_shift":
					if (e.Count != 3) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (ColorShift != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					ColorShift = new Vector3(e[0].ToSingle(), e[1].ToSingle(), e[2].ToSingle());
					break;
				case "ambient_intensity":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (AmbientIntensity != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					AmbientIntensity = e[0].ToSingle();
					break;
				case "cull_mode":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (CullMode != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					CullMode = e[0].ToInt32();
					break;
				case "vert_increase":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (VertIncrease != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					VertIncrease = e[0].ToInt32();
					break;
				default:
					throw new Exception("Invalid Entry in " + section.Name + ": " + e.Name);
				}
			}
		}
	}
}