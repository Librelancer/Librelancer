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
	public class UIServerDescription : HudModelElement
	{
		public string Description = "";
		public UIServerList ServerList;
        Font fntTitle;
        Font fntContent;
		public UIServerDescription(UIManager manager, float x, float y) : base(manager, "../INTRO/OBJECTS/front_serverselect_info.cmp",x,y, 1.93f, 2.65f)
		{
            fntTitle = manager.Game.Fonts.GetSystemFont("Agency FB");
            fntContent = manager.Game.Fonts.GetSystemFont("Arial Unicode MS");
		}

		public override void DrawText()
		{
			if (ServerList == null) return;
			var rect = GetTextRectangle();
            var titleSz = Manager.ButtonFontSize;

			var rTitle = new Rectangle(rect.X, rect.Y, rect.Width, (int)fntTitle.LineHeight(titleSz));
			var rContent = new Rectangle(rect.X, rect.Y + (int)fntTitle.LineHeight(titleSz), rect.Width, (int)fntContent.LineHeight(12));

			DrawTextCentered(fntTitle, titleSz, "SERVER DESCRIPTION", rTitle, Manager.TextColor);
			if (string.IsNullOrEmpty(Description)) return;
			int a, b = 0;
			var strs = Infocards.InfocardDisplay.WrapText(Manager.Game.Renderer2D, fntContent, 12, Description, rect.Width, 0, out a, ref b);
			foreach (var ln in strs)
			{
				DrawTextCentered(fntContent, 12, ln, rContent, Manager.TextColor);
                rect.Y += (int)fntContent.LineHeight(12);
			}
		}

		Rectangle GetTextRectangle()
		{
			var tl = IdentityCamera.Instance.ScreenToPixel(Position.X - 0.33f * Scale.X, Position.Y + 0.045f * Scale.Y);
			var br = IdentityCamera.Instance.ScreenToPixel(Position.X + 0.33f * Scale.X, Position.Y - 0.05f * Scale.Y);
			return new Rectangle(
				(int)tl.X,
				(int)tl.Y,
				(int)(br.X - tl.X),
				(int)(br.Y - tl.Y)
			);
		}
	}
}
