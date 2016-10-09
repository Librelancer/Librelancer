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
	public class Cursor
	{
		public string Nickname;
		public float Blend; //TODO: What is this?
		public float Spin = 0;
		public float Scale = 1;
		public Vector2 Hotspot = Vector2.Zero;
		public Color4 Color = Color4.White;
		public string Shape;
		public Cursor(Section s)
		{
			foreach (var e in s)
			{
				switch (e.Name.ToLowerInvariant())
				{
					case "nickname":
						Nickname = e[0].ToString();
						break;
					case "anim":
						Shape = e[0].ToString(); //TODO: mouse.ini cursor anim
						break;
					case "blend":
						Blend = e[0].ToSingle();
						break;
					case "spin":
						Spin = e[0].ToSingle();
						break;
					case "scale":
						Scale = e[0].ToSingle();
						break;
					case "hotspot":
						Hotspot = new Vector2(e[0].ToSingle(), e[1].ToSingle());
						break;
					case "color":
						Color = new Color4(e[0].ToInt32() / 255f, e[1].ToInt32() / 255f, e[2].ToInt32() / 255f, e[3].ToInt32() / 255f);
						break;
				}
			}
		}
	}
}
