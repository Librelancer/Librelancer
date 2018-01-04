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
	public class ButtonElement2D : Element2D
	{
		const int FNT_SIZE = 10;

		public float Width;
		public float Height;
		public string Label = "";
		Font fnt;

		public void AutoSize(Renderer2D ren)
		{
			var sz = ren.MeasureString(fnt, FNT_SIZE, Label);
			Width = sz.X + 18;
			Height = fnt.LineHeight(FNT_SIZE) + 5;
		}

		public ButtonElement2D(UIManager m, Font uiFont) : base(m)
		{
			fnt = uiFont;
		}

		public override void DrawText()
		{
			Rectangle r;
			TryGetHitRectangle(out r);
			Manager.Game.Renderer2D.FillRectangle(
				r,
				Color4.LightGray);
			Manager.Game.Renderer2D.DrawRectangle(
				r,
				Color4.Black,
				1);
			var sz = Manager.Game.Renderer2D.MeasureString(fnt, FNT_SIZE, Label);
			float xPos = ClientPosition.X + (Width / 2) - sz.X / 2;
			float yPos = ClientPosition.Y + (Height / 2) - (fnt.LineHeight(FNT_SIZE) / 2);
			Manager.Game.Renderer2D.DrawStringBaseline(fnt, FNT_SIZE, Label, xPos, yPos, 0, Color4.Black);
		}

		public Action Clicked;
		public override void WasClicked()
		{
			Clicked();
		}

		public override bool TryGetHitRectangle(out Rectangle rect)
		{
			rect = new Rectangle(
				(int)ClientPosition.X,
				(int)ClientPosition.Y,
				(int)Width,
				(int)Height
			);
			return true;
		}
	}
}
