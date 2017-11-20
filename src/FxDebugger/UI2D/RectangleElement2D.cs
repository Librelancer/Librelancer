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
 * Portions created by the Initial Developer are Copyright (C) 2013-2017
 * the Initial Developer. All Rights Reserved.
 */
using System;
namespace LibreLancer.FxDebugger
{
	class RectangleElement2D : Element2D
	{
		public Color4 FillColor = Color4.DarkGray;
		public bool Fullscreen;
		public float Width;
		public float Height;

		public RectangleElement2D(UIManager m) : base(m) { }

		public override void DrawText()
		{
			Rectangle r = new Rectangle(
				(int)Position2D.X,
				(int)Position2D.Y,
				(int)Width,
				(int)Height
			);
			if (Fullscreen) r = new Rectangle(0, 0, (int)Manager.Game.Width, (int)Manager.Game.Height);
			Manager.Game.Renderer2D.FillRectangle(r, FillColor);
		}
	}
}
