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
namespace LibreLancer
{
	public class HudChatBox : UIElement
	{
		Font boldFont;
		Font regularFont;
		public string CurrentEntry = "Console->";
		public string CurrentText = "";
		public int MaxChars = 100;
		public Color4 Background = new Color4(0.1f, 0.1f, 0.4f, 0.5f);
		public Color4 Border = Color4.LightGreen;
		public Color4 TextColor = Color4.White;
		public bool CentreScreen = false;

		public HudChatBox(UIManager m) : base(m) 
		{
			var fontSizePx = (GetBaseRectangle().Height / 3.2f);
			var textSize = GetTextSize(fontSizePx);
			boldFont = m.Game.Fonts.GetSystemFont("Arial Unicode MS", FontStyles.Bold);
			regularFont = m.Game.Fonts.GetSystemFont("Arial Unicode MS", FontStyles.Regular);
		}

		public bool AppendText(string str)
		{
			if (CurrentText.Length + str.Length > MaxChars)
				return false;
			CurrentText += str;
			return true;
		}

		public override void DrawBase()
		{

		}

		public override void DrawText()
		{
			var r = GetBaseRectangle();
			Manager.Game.Renderer2D.FillRectangle(r, Background);
			Manager.Game.Renderer2D.DrawRectangle(r, Border, 1);

			var fontSizePx = r.Height / 3.2f;
			var textSize = GetTextSize(fontSizePx);
			var measured = Manager.Game.Renderer2D.MeasureString(boldFont, textSize, CurrentEntry);

			Manager.Game.Renderer2D.DrawWithClip(r, () =>
			{
				Manager.Game.Renderer2D.DrawStringBaseline(boldFont, textSize, CurrentEntry, r.X + 3, r.Y + 1, r.X + 3, Color4.Black, false);
				Manager.Game.Renderer2D.DrawStringBaseline(boldFont, textSize, CurrentEntry, r.X + 2, r.Y + 1, r.X + 2, TextColor, false);
				int a;
				int dY = 0;
				var str = string.Join("\n",
									  Infocards.InfocardDisplay.WrapText(
										  Manager.Game.Renderer2D,
										  regularFont,
					                      (int)textSize,
										  CurrentText,
										  r.Width - 2,
										  measured.X,
										  out a,
										  ref dY)
									 );
				Manager.Game.Renderer2D.DrawStringBaseline(regularFont, textSize, str, r.X + 3 + measured.X, r.Y + 1, r.X + 3, Color4.Black, false);
				Manager.Game.Renderer2D.DrawStringBaseline(regularFont, textSize, str, r.X + 2 + measured.X, r.Y + 1, r.X + 2, TextColor, false);
			});
		}


		float GetTextSize(float px)
		{
			return (int)Math.Floor((px * (72.0f / 96.0f)) - 2);
		}

		Rectangle GetBaseRectangle()
		{
			float xScale = 1;
			float screenAspect = Manager.Game.Width / (float)Manager.Game.Height;
			float uiAspect = 4f / 3f;
			if (screenAspect > uiAspect)
				xScale = uiAspect / screenAspect;
			float w = Manager.Game.Width * xScale;
			float h = Manager.Game.Height;

			var posY = CentreScreen ? (h / 2) - (h * 0.086f) / 2 : (h - (h * 0.18f));
				return new Rectangle((int)(Manager.Game.Width / 2f - (w * 0.44f) / 2),
				                     (int)(posY),
				                     (int)(w * 0.44f),
				                     (int)(h * 0.086f));
		}
	}
}
