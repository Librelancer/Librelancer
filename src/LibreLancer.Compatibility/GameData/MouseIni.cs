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
using LibreLancer.Ini;
namespace LibreLancer.Compatibility.GameData
{
	public class MouseIni : IniFile
	{
		public string TxmFile;
		public string TextureName;
		public List<MouseShape> Shapes = new List<MouseShape>();
		public List<Cursor> Cursors = new List<Cursor>();
		public MouseIni(string filename)
		{
			foreach (Section s in ParseFile(filename))
			{
				switch (s.Name.ToLowerInvariant())
				{
					case "texture":
						foreach (var e in s)
						{
							switch (e.Name.ToLowerInvariant())
							{
								case "file":
									TxmFile = e[0].ToString();
									break;
								case "name":
									TextureName = e[0].ToString();
									break;
							}
						}
						break;
					case "shape":
						Shapes.Add(new MouseShape(s));
						break;
					case "cursor":
						Cursors.Add(new Cursor(s));
						break;
				}
			}
		}
	}

}
