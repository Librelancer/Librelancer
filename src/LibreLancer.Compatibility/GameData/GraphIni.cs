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
namespace LibreLancer.Compatibility.GameData
{
	public class GraphIni : IniFile
	{
		public List<FloatGraph> FloatGraphs = new List<FloatGraph>();

		public FloatGraph FindFloatGraph(string nickname)
		{
			var result = from FloatGraph s in FloatGraphs where s.Name.ToLowerInvariant() == nickname.ToLowerInvariant() select s;
			if (result.Count() == 1) return result.First();
			else return null;
		}

		public void AddGraphIni(string path)
		{
			foreach (var section in ParseFile(path))
			{
				if (section.Name.ToLowerInvariant() != "igraph")
					throw new Exception("Unexpected section in Graph ini: " + section.Name);
				string nickname = null;
				FloatGraph fg = null;
				bool skip = false;
				foreach (var e in section)
				{
					if (skip)
						break;
					switch (e.Name.ToLowerInvariant())
					{
						case "nickname":
							nickname = e[0].ToString();
							break;
						case "type":
							var t = e[0].ToString().ToUpperInvariant();
							if (t == "FLOAT")
								fg = new FloatGraph();
							else
								skip = true;
							break;
						case "point":
							if (fg == null)
								throw new Exception("Point appearing after type");
							fg.Points.Add(
								new Vector2(
								e[0].ToInt32(),
								e[1].ToSingle()
								)
							);
							break;
					}
				}
				if (skip)
					continue;
				fg.Name = nickname;
				FloatGraphs.Add(fg);
			}
		}
	}
}

