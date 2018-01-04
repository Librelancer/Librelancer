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
	public class UIMenuButton : UIElement
	{
		public string Text = "";
		Color4 color;
		Font buttonFont;

		public UIMenuButton (UIManager manager, Vector2 position, string text, string tag = null) : base(manager)
		{
			UIScale = new Vector2 (1.86f, 2.73f);
			Text = text;
			UIPosition = position;
			Tag = tag;
			color = manager.TextColor;
			buttonFont = manager.Game.Fonts.GetSystemFont("Agency FB");
		}

		public override void DrawBase ()
		{
			Manager.MenuButton.Draw (
				Manager.Game.RenderState,
				GetWorld (UIScale, Position),
				Lighting.Empty
			);
		}
		protected override void UpdateInternal (TimeSpan time)
		{
			var rect = GetTextRectangle ();
			color = Tag != null ? Manager.TextColor : Color4.Gray;
			if (rect.Contains (Manager.Game.Mouse.X, Manager.Game.Mouse.Y) && Tag != null) {
				color = GetPulseColor();
			}
		}

		public override bool TryGetHitRectangle(out Rectangle rect)
		{
			rect = GetTextRectangle();
			return true;
		}

		public override void WasClicked()
		{
			if (Tag != null) Manager.OnClick(Tag);
		}

		public override void DrawText()
		{
			var r = GetTextRectangle ();
			var sz = GetTextSize (r.Height);
			DrawTextCentered (buttonFont, sz, Text, r, color);
		}

		float GetTextSize (float px)
		{
			return (int)Math.Floor ((px * (72.0f / 96.0f)) - 11);
		}
		Color4 GetPulseColor()
		{
			//TODO: Made this function playing around in GeoGebra. Probably not great
			double pulseState = Math.Abs(Math.Cos(9 * Manager.Game.TotalTime));
			var a = new Color3f(Manager.TextColor.R, Manager.TextColor.G, Manager.TextColor.B);
			var b = new Color3f(Color4.Yellow.R, Color4.Yellow.G, Color4.Yellow.B);
			var result =  Utf.Ale.AlchemyEasing.EaseColorRGB(
				Utf.Ale.EasingTypes.Linear,
				(float)pulseState,
				0,
				1,
				a,
				b
			);
			return new Color4(result.R, result.G, result.B, 1);
		}
		Rectangle GetTextRectangle ()
		{
			var topleft = Manager.ScreenToPixel (Position.X - 0.125f * UIScale.X, Position.Y + 0.02f * UIScale.Y);
			var bottomRight = Manager.ScreenToPixel (Position.X + 0.125f * UIScale.X, Position.Y - 0.02f * UIScale.Y);
			var rect = new Rectangle (
				(int)topleft.X,
				(int)topleft.Y,
				(int)(bottomRight.X - topleft.X),
				(int)(bottomRight.Y - topleft.Y)
			);
			return rect;
		}

	}
}

