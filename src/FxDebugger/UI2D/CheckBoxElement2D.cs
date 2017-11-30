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
	public class CheckBoxElement2D : Element2D
	{
		public bool Checked = true;
		public bool InView = true;
		public Action Clicked;

		public CheckBoxElement2D(UIManager m) : base(m) { }

		public override void DrawText()
		{
			Rectangle r;
			TryGetHitRectangle(out r);
			Manager.Game.Renderer2D.FillRectangle(r, Color4.LightGray);
			Manager.Game.Renderer2D.DrawRectangle(r, Color4.Black, 1);
			if (Checked)
			{
				Manager.Game.Renderer2D.DrawLine(Color4.Black, ClientPosition + new Vector2(2), ClientPosition + new Vector2(14));
				Manager.Game.Renderer2D.DrawLine(Color4.Black, ClientPosition + new Vector2(2, 14), ClientPosition + new Vector2(14, 2));
			}
		}

		public override void WasClicked()
		{
			Checked = !Checked;
			if(Clicked != null) Clicked();
		}

		public override bool TryGetHitRectangle(out Rectangle rect)
		{
			rect = new Rectangle(
				(int)ClientPosition.X,
				(int)ClientPosition.Y,
				16,
				16
			);
			return InView;
		}
	}
}
