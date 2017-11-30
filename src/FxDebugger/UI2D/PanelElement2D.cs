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
using System.Collections.Generic;
namespace LibreLancer.FxDebugger
{
	class PanelElement2D : Element2D, IUIContainer
	{
		public Color4 FillColor = Color4.DarkGray;
		public bool Fullscreen;
		public float Width;
		public float Height;
		public List<Element2D> Children = new List<Element2D>();
		public PanelElement2D(UIManager m) : base(m) { }

		protected override void UpdateInternal(TimeSpan time)
		{
			base.UpdateInternal(time);
			foreach (var child in Children)
			{
				child.Offset2D = (Position2D + Offset2D);
				child.Update(time);
			}
		}

		public override void DrawText()
		{
			Rectangle r = new Rectangle(
				(int)ClientPosition.X,
				(int)ClientPosition.Y,
				(int)Width,
				(int)Height
			);
			if (Fullscreen) r = new Rectangle(0, 0, (int)Manager.Game.Width, (int)Manager.Game.Height);
			Manager.Game.Renderer2D.FillRectangle(r, FillColor);

			foreach (var child in Children)
			{
				child.DrawText();
			}
		}

		public IEnumerable<UIElement> GetChildren()
		{
			return Children;
		}
	}
}
