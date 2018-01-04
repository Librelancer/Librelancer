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
		public UIServerDescription(UIManager manager, float x, float y) : base(manager, "../INTRO/OBJECTS/front_serverselect_info.cmp",x,y, 1.93f, 2.65f)
		{
		}

		public override void DrawText()
		{
			/*if (ServerList == null) return;
			var rect = GetTextRectangle();


			var rTitle = new Rectangle(rect.X, rect.Y, rect.Width, (int)fnts.HeaderFont.LineHeight);
			var rContent = new Rectangle(rect.X, rect.Y + (int)fnts.HeaderFont.LineHeight, rect.Width, (int)fnts.ContentFont.LineHeight);

			DrawTextCentered(fnts.HeaderFont, "SERVER DESCRIPTION", rTitle, Manager.TextColor);
			if (string.IsNullOrEmpty(Description)) return;
			int a, b = 0;
			var strs = Infocards.InfocardDisplay.WrapText(Manager.Game.Renderer2D, fnts.ContentFont, Description, rect.Width, 0, out a, ref b);
			foreach (var ln in strs)
			{
				DrawTextCentered(fnts.ContentFont, ln, rContent, Manager.TextColor);
				rect.Y += (int)fnts.ContentFont.LineHeight;
			}*/
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
