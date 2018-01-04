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
	public class FloatingCheckList : Element2D, IUIContainer
	{
		const int FNT_SIZE = 10;
		List<ListItem> items = new List<ListItem>();
		Font fnt;
		public string Title;
		public float Width;
		public float Height;
		public float MinYPosition;
		public bool Collapsed = false;
		float scrollOffset = 0;
		CollapseCell collapser;

		public FloatingCheckList(UIManager m, Font uiFont) : base(m)
		{
			fnt = uiFont;
			collapser = new CollapseCell(m) { Position2D = new Vector2(120, 2) };
			collapser.Clicked += () =>
			{
				collapser.Collapsed = Collapsed = !Collapsed;
			};
		}

		public void AddNode(string name)
		{
			var lbl = new LabelElement2D(Manager, fnt) { Text = name };
			var chk = new CheckBoxElement2D(Manager);
			var more = new MoreCell(Manager);
			chk.Clicked += () =>
			{
				if (SetActive != null) SetActive(name, chk.Checked);
			};
			more.Clicked += () =>
			{
				if (OpenMenu != null) OpenMenu(name);
			};
			items.Add(new ListItem() { Active = chk, Name = lbl, More = more });
		}

		public event Action<string, bool> SetActive;
		public event Action<string> OpenMenu;
		bool lastDown = false;
		bool dragging = false;
		Vector2 dragOffset;
		protected override void UpdateInternal(TimeSpan time)
		{
			base.UpdateInternal(time);
			//Title Bar
			var mouse = Manager.Game.Mouse;
			if (!mouse.IsButtonDown(MouseButtons.Left))
			{
				dragging = false;
				lastDown = false;
			}
			if (mouse.IsButtonDown(MouseButtons.Left) && dragging)
			{
				Position2D = new Vector2(mouse.X, mouse.Y) - dragOffset;
				if (Position2D.X < 0) Position2D.X = 0;
				if (Position2D.Y < MinYPosition) Position2D.Y = MinYPosition;
			}
			if (mouse.IsButtonDown(MouseButtons.Left) && !lastDown)
			{
				lastDown = true;
				var dragRect = new Rectangle((int)ClientPosition.X,
							  (int)ClientPosition.Y,
							  (int)Width,
							  (int)25);
				if (dragRect.Contains(mouse.X, mouse.Y))
				{
					dragging = true;
					dragOffset = new Vector2(mouse.X, mouse.Y) - ClientPosition;
				}
			}
			//Manage Children
			collapser.Offset2D = ClientPosition;
			collapser.Position2D = new Vector2(Width - 20, (25f / 2) - 8);
			var y = 27;
			var visibleRect = new Rectangle((int)ClientPosition.X,
							  (int)ClientPosition.Y + 25,
							  (int)Width,
							  (int)Height - 25);
			foreach (var itm in items)
			{
				itm.Name.Offset2D = ClientPosition;
				itm.Active.Offset2D = ClientPosition;
				itm.More.Offset2D = ClientPosition;

				itm.More.Position2D = new Vector2(22, y);
				itm.Name.Position2D = new Vector2(40, y);
				itm.Active.Position2D = new Vector2(2, y);
				Rectangle hit;
				itm.Active.TryGetHitRectangle(out hit);
				itm.Active.InView = visibleRect.Intersects(hit);
				itm.More.TryGetHitRectangle(out hit);
				itm.More.InView = visibleRect.Intersects(hit);
				y += (int)(fnt.LineHeight(FNT_SIZE) + 3);
			}
		}

		public override void DrawText()
		{
			//Background
			if (!Collapsed)
			{
				Manager.Game.Renderer2D.FillRectangle(
					new Rectangle((int)ClientPosition.X,
								  (int)ClientPosition.Y,
								  (int)Width,
								  (int)Height),
					Color4.White);
			}
			//Grab bar
			Manager.Game.Renderer2D.FillRectangle(
				new Rectangle((int)ClientPosition.X,
							  (int)ClientPosition.Y,
							  (int)Width,
							  (int)25),
				Color4.DarkGray);

			Manager.Game.Renderer2D.DrawStringBaseline(fnt, FNT_SIZE, Title, ClientPosition.X + 3, ClientPosition.Y + 3, 0, Color4.Black);
			//Collapse button
			collapser.DrawText();
			//Children
			if (!Collapsed)
			{
				Manager.Game.Renderer2D.DrawWithClip(
					new Rectangle((int)ClientPosition.X,
								  (int)ClientPosition.Y + 25,
								  (int)Width,
								  (int)Height - 25)
					, () =>
					{
						foreach (var itm in items)
						{
							itm.Name.DrawText();
							itm.Active.DrawText();
							itm.More.DrawText();
						}
					});
			}
		}

		public IEnumerable<UIElement> GetChildren()
		{
			yield return collapser;
			if (!Collapsed)
			{
				foreach (var itm in items)
				{
					yield return itm.More;
					yield return itm.Active;
				}
			}
		}

		float CalculateHeight()
		{
			return items.Count * fnt.LineHeight(FNT_SIZE);
		}

		class ListItem
		{
			public CheckBoxElement2D Active;
			public MoreCell More;
			public LabelElement2D Name;
		}

		class CollapseCell : Element2D
		{
			public bool Collapsed = false;
			public Action Clicked;

			public CollapseCell(UIManager m) : base(m) { }

			public override void DrawText()
			{
				Rectangle r;
				TryGetHitRectangle(out r);
				Manager.Game.Renderer2D.FillRectangle(r, Color4.LightGray);
				Manager.Game.Renderer2D.DrawRectangle(r, Color4.Black, 1);
				if (Collapsed)
				{
					Manager.Game.Renderer2D.FillTriangle(
						ClientPosition + new Vector2(8, 13),
						ClientPosition + new Vector2(3, 5),
						ClientPosition + new Vector2(13, 5),
						Color4.Black
					);
				}
				else
				{
					Manager.Game.Renderer2D.FillTriangle(
						ClientPosition + new Vector2(8, 3),
						ClientPosition + new Vector2(3, 11),
						ClientPosition + new Vector2(13, 11),
						Color4.Black
					);
				}
			}

			public override void WasClicked()
			{
				Clicked();
			}

			public override bool TryGetHitRectangle(out Rectangle rect)
			{
				rect = new Rectangle(
					(int)ClientPosition.X,
					(int)ClientPosition.Y,
					16,
					16
				);
				return true;
			}
		}
		class MoreCell : Element2D
		{
			public bool InView = true;
			public MoreCell(UIManager m) : base(m) { }
			public Action Clicked;

			public override void DrawText()
			{
				Rectangle r;
				TryGetHitRectangle(out r);
				Manager.Game.Renderer2D.FillRectangle(r, Color4.LightGray);
				Manager.Game.Renderer2D.DrawRectangle(r, Color4.Black, 1);

				Manager.Game.Renderer2D.DrawLine(Color4.Black, ClientPosition + new Vector2(2,4), ClientPosition + new Vector2(14,4));
				Manager.Game.Renderer2D.DrawLine(Color4.Black, ClientPosition + new Vector2(2,7), ClientPosition + new Vector2(14, 7));
				Manager.Game.Renderer2D.DrawLine(Color4.Black, ClientPosition + new Vector2(2,10), ClientPosition + new Vector2(14, 10));
			}

			public override void WasClicked()
			{
				Clicked();
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
}
