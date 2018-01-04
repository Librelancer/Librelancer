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
	public class UICharacterList : HudModelElement
	{
		const int NUM_ROWS = 12;

		float[] dividerPositions = {
			0.3f,
			0.4f,
			0.53f,
			0.78f
		};

		class CharacterContent : IGridContent
		{
			UICharacterList list;
			public CharacterContent(UICharacterList lst)
			{
				list = lst;
			}

			public int Count
			{
				get
				{
					return 0;
				}
			}

			public int Selected
			{
				get
				{
					return -1;
				}

				set
				{
					//throw new NotImplementedException();
				}
			}

			public string GetContentString(int row, int column)
			{
				return null;
			}
		}

		GridControl grid;
		public UICharacterList(UIManager manager) : base(manager, "../INTRO/OBJECTS/front_characselectbox.cmp", 0.3f, 0f, 1.61f, 2.27f)
		{
			var columnNames = new string[] {
				"CHARACTER NAME", "RANK", "FUNDS", "SHIP TYPE", "LOCATION"
			};
			grid = new GridControl(manager, dividerPositions, columnNames, GetRectangle, new CharacterContent(this), NUM_ROWS);
		}

		protected override void UpdateInternal(TimeSpan time)
		{
			grid.Update();
		}
		public override void DrawText()
		{
			grid.Draw();
		}
		Rectangle GetRectangle()
		{
			var tl = IdentityCamera.Instance.ScreenToPixel(Position.X - 0.38f * Scale.X, Position.Y + 0.195f * Scale.Y);
			var br = IdentityCamera.Instance.ScreenToPixel(Position.X + 0.38f * Scale.X, Position.Y - 0.232f * Scale.Y);
			return new Rectangle(
				(int)tl.X,
				(int)tl.Y,
				(int)(br.X - tl.X),
				(int)(br.Y - tl.Y)
			);
		}
	}
}
