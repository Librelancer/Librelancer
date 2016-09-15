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
using System.Linq;
using LibreLancer.Ini;
namespace LibreLancer.Compatibility.GameData.Effects
{
	public class EffectsIni : IniFile
	{
		public List<VisEffect> VisEffects = new List<VisEffect>();
		public List<Effect> Effects = new List<Effect>();

		public void AddIni(string ini)
		{
			foreach (Section s in ParseFile(ini))
			{
				switch (s.Name.ToLowerInvariant())
				{
					case "viseffect":
						VisEffects.Add(new VisEffect(s));
						break;
					case "effect":
						Effects.Add(new Effect(s));
						break;
				}
			}
		}

		public Effect FindEffect(string nickname)
		{
			var result = from Effect e in Effects where e.Nickname.Equals(nickname,StringComparison.OrdinalIgnoreCase) select e;
			if (result.Count() == 1)
				return result.First();
			throw new Exception();
		}

		public VisEffect FindVisEffect(string nickname)
		{
			var result = from VisEffect v in VisEffects where v.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase) select v;
			if (result.Count() == 1)
				return result.First();
			throw new Exception();
		}
	}
}
