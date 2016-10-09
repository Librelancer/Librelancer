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
namespace LibreLancer
{
	public class Cursor
	{
		public string Nickname;
		public string Texture;
		public Rectangle Dimensions;
		public float Spin;
		public float Scale;
		public Color4 Color = Color4.White;
		public Vector2 Hotspot = Vector2.Zero;
		public ResourceManager Resources;

		public void Draw(Renderer2D renderer, Mouse m)
		{
			var pos = new Vector2(m.X, m.Y) - Hotspot;
			var dst = new Rectangle(
				(int)pos.X, (int)pos.Y,
				(int)(Dimensions.Width * Scale), (int)(Dimensions.Height * Scale)
			);
			renderer.Draw(
				(Texture2D)Resources.FindTexture(Texture),
				Dimensions,
				dst,
				Color,
				BlendMode.Additive
			);
		}
	}
}
