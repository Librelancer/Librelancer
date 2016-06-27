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
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
using System;
using LibreLancer.Ini;
namespace LibreLancer.Compatibility.GameData.Solar
{
	public class LensGlow
	{
		public string Nickname;
		public string Shape;
		public int RadiusScale;
		public Color3f InnerColor;
		public Color3f OuterColor;
		public float GlowFadeInSeconds;
		public float GlowFadeOutSeconds;

		public LensGlow(Section s)
		{
			foreach (Entry e in s)
			{
				switch (e.Name.ToLowerInvariant())
				{
					case "nickname":
						Nickname = e[0].ToString();
						break;
					case "shape":
						Shape = e[0].ToString();
						break;
					case "radius_scale":
						RadiusScale = e[0].ToInt32();
						break;
					case "inner_color":
						InnerColor = new Color3f(e[0].ToSingle(), e[1].ToSingle(), e[2].ToSingle());
						break;
					case "outer_color":
						OuterColor = new Color3f(e[0].ToSingle(), e[1].ToSingle(), e[2].ToSingle());
						break;
					case "glow_fade_in_seconds":
						GlowFadeInSeconds = e[0].ToSingle();
						break;
					case "glow_fade_out_seconds":
						GlowFadeOutSeconds = e[0].ToSingle();
						break;
				}
			}
		}
	}
}

