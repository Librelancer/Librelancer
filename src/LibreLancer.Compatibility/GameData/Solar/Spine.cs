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
	public class Spine
	{
		//FORMAT: LengthScale, WidthScale, [Inner: r, g, b], [Outer: r, g, b], Alpha

		public float LengthScale;
		public float WidthScale;
		public Color3f InnerColor;
		public Color3f OuterColor;
		public float Alpha;

		public Spine(Entry e)
		{
			LengthScale = e[0].ToSingle();
			WidthScale = e[1].ToSingle();
			InnerColor = new Color3f(e[2].ToSingle(), e[3].ToSingle(), e[4].ToSingle());
			OuterColor = new Color3f(e[5].ToSingle(), e[6].ToSingle(), e[7].ToSingle());
			Alpha = e[8].ToSingle();
		}
	}
}

