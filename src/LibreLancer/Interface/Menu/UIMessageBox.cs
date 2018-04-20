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
using LibreLancer.Infocards;
namespace LibreLancer
{
	public class UIMessageBox : HudModelElement
	{
		Infocard ifc;
		public UIMessageBox(UIManager m, int infocard) : base(m, "../NEURONET/errorexplanation.cmp", 0, 0, 1.25f, 1.65f)
		{
			ifc = m.Game.GameData.GetInfocard(infocard);
            SetFormatting();
		}
        public UIMessageBox(UIManager m, Infocard infocard) : base(m, "../NEURONET/errorexplanation.cmp", 0, 0, 1.25f, 1.65f)
        {
            ifc = infocard;
            SetFormatting();
        }
        void SetFormatting()
        {
            foreach (var n in ifc.Nodes)
            {
                if (n is InfocardTextNode)
                {
                    //TODO: FL probably just alters the defaults when parsing, but this works for all the vanilla
                    //GUI infocards anyway
                    var t = (InfocardTextNode)n;
                    t.Alignment = TextAlignment.Center;
                    t.Color = Manager.TextColor;
                    t.FontIndex = -1;
                }
            }
        }

		Rectangle oldRect = new Rectangle(0, 0, 0, 0);
		Rectangle GetInfocardRect()
		{
			var tl = IdentityCamera.Instance.ScreenToPixel(-0.61f, 0.30f);
			var br = IdentityCamera.Instance.ScreenToPixel(0.61f, -0.21f);
			return new Rectangle(
				(int)tl.X,
				(int)tl.Y,
				(int)(br.X - tl.X),
				(int)(br.Y - tl.Y)
			);
		}
		InfocardDisplay text;
		public override void DrawText()
		{
			if (text == null)
			{
				var r = GetInfocardRect();
				oldRect = r;
				text = new InfocardDisplay(Manager.Game, r, ifc) { DropShadow = true };
			}
			var rect = GetInfocardRect();
			if (rect != oldRect)
			{
				oldRect = rect;
				text.SetRectangle(rect);
			}
			text.Draw(Manager.Game.Renderer2D);
		}
	}
}
