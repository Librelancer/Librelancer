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
using System.Collections.Generic;
using LibreLancer.Ini;
namespace LibreLancer.Compatibility.GameData.Effects
{
	public class VisEffect
	{
		public string Nickname;
		public int EffectCrc;
		public string AlchemyPath;
		public List<string> Textures = new List<string>();
		public VisEffect(Section s)
		{
			foreach (var e in s)
			{
				switch (e.Name.ToLowerInvariant())
				{
					case "nickname":
						Nickname = e[0].ToString();
						break;
					case "alchemy":
						AlchemyPath = e[0].ToString();
						break;
					case "textures":
						Textures.Add(e[0].ToString());
						break;
					case "effect_crc":
						EffectCrc = e[0].ToInt32();
						break;
						
				}
			}
		}
	}
}
