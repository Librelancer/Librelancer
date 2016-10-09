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
namespace LibreLancer.Compatibility.GameData
{
	public class MouseShape
	{
		public string Name;
		public Rectangle Dimensions;
		public MouseShape(Section s)
		{
			int x = 0, y = 0, w = 0, h = 0;
			foreach (var e in s)
			{
				switch (e.Name.ToLowerInvariant())
				{
					case "name":
						Name = e[0].ToString();
						break;
					case "x":
						x = e[0].ToInt32();
						break;
					case "y":
						y = e[0].ToInt32();
						break;
					case "w":
						w = e[0].ToInt32();
						break;
					case "h":
						h = e[0].ToInt32();
						break;
				}
			}
			Dimensions = new Rectangle(x, y, w, h);
		}
	}
}
