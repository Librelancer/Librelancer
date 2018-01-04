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
	public class SliderElement2D : Element2D
	{
		const int FNT_SIZE = 10;

		public float Value = 1000;
		public float Minimum = 10;
		public float Maximum = 4000;

		public float Width;
		public float Height;

		public string Label;

		Font fnt;


		public void AutoSize(Renderer2D renderer)
		{
			var sz = renderer.MeasureString(fnt, FNT_SIZE, Label);
			Width = sz.X + 240;
			Height = fnt.LineHeight(FNT_SIZE) + 5;	
		}

		public SliderElement2D(UIManager manager, Font uiFont) : base(manager)
		{
			fnt = uiFont;
		}

		protected override void UpdateInternal(TimeSpan time)
		{
			if (CalculatePosition != null) CalculatePosition();
			if (!Manager.Game.Mouse.IsButtonDown(MouseButtons.Left)) return;
			var lblSz = Manager.Game.Renderer2D.MeasureString(fnt, FNT_SIZE, Label);
			var startX = ClientPosition.X + lblSz.X + 7;
			var startY = ClientPosition.Y + (Height / 2) - (Height * 0.4f);
			var sliderW = Width - (lblSz.X + 7);
			var height = Height * 0.8f;
			var rect = new Rectangle((int)startX, (int)startY, (int)sliderW, (int)height);
			if (rect.Contains(Manager.Game.Mouse.X, Manager.Game.Mouse.Y))
			{
				var offsetPx = Manager.Game.Mouse.X - startX;
				var pct = offsetPx / sliderW;
				Value = Minimum + ((Maximum - Minimum) * pct);
			}
		}

		public bool BlackText = false;
		public override void DrawText()
		{
			float yPos = ClientPosition.Y + (Height / 2) - (fnt.LineHeight(FNT_SIZE) / 2);
			if (BlackText)
			{
				Manager.Game.Renderer2D.DrawStringBaseline(fnt, FNT_SIZE, Label, ClientPosition.X, yPos, 0, Color4.Black);
			}
			else
			{
				//Shadowed label
				Manager.Game.Renderer2D.DrawStringBaseline(fnt, FNT_SIZE, Label, ClientPosition.X + 1, yPos + 1, 0, Color4.Black);
				Manager.Game.Renderer2D.DrawStringBaseline(fnt, FNT_SIZE, Label, ClientPosition.X, yPos, 0, Color4.White);
			}
			//Draw Slider
			var lblSz = Manager.Game.Renderer2D.MeasureString(fnt, FNT_SIZE, Label);
			//track
			var sliderX = lblSz.X + 7;
			var sliderW = Width - sliderX;
			Manager.Game.Renderer2D.FillRectangle(new Rectangle(
				(int)(ClientPosition.X + sliderX),
				(int)(ClientPosition.Y + (Height / 2) - 1),
				(int)sliderW,
				2), Color4.Gray);
			//thumb
			var xPos = ClientPosition.X + sliderX + ((Value / (Maximum + Minimum)) * sliderW) - 6;
			var off = ((Value / Maximum) * sliderW);
			yPos = ClientPosition.Y + (Height / 2) - (Height * 0.4f);

			Manager.Game.Renderer2D.FillRectangle(new Rectangle(
			(int)(xPos),
			(int)(yPos),
			(int)12,
			(int)(Height * 0.8f)), Color4.LightGray);
		}
	}
}
