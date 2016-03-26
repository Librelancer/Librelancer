using System;
using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;

namespace LibreLancer
{
	public class UIMenuButton : UIElement
	{
		public string Text = "";
		Color4 color;

		public UIMenuButton (UIManager manager, Vector2 position, string text, string tag = null) : base(manager)
		{
			UIScale = new Vector2 (2f, 3f);
			Text = text;
			UIPosition = position;
			Tag = tag;
			color = manager.TextColor;
		}

		public override void DrawBase ()
		{
			Manager.MenuButton.Draw (
				Manager.Game.RenderState,
				GetWorld (UIScale, Position),
				Lighting.Empty
			);
		}
		public override void Update (TimeSpan time)
		{
			var mstate = Manager.Game.Mouse.GetCursorState ();
			var rect = GetTextRectangle ();
			color = Tag != null ? Manager.TextColor : Color4.Gray;
			if (rect.Contains (mstate.X, mstate.Y) && Tag != null) {
				color = Color4.Yellow;
				if (mstate.IsButtonDown (MouseButton.Left)) {
					Manager.OnClick (Tag);
				}
			}
		}
		public override void DrawText()
		{
			var r = GetTextRectangle ();
			var sz = GetTextSize (r.Height);
			DrawTextCentered (Manager.GetButtonFont (sz), Text, r, color);
		}

		float GetTextSize (float px)
		{
			return (int)Math.Floor ((px * (72.0f / 96.0f)) - 14);
		}

		Rectangle GetTextRectangle ()
		{
			var topleft = Manager.ScreenToPixel (Position.X - 0.125f * UIScale.X, Position.Y + 0.022f * UIScale.Y);
			var bottomRight = Manager.ScreenToPixel (Position.X + 0.125f * UIScale.X, Position.Y - 0.022f * UIScale.Y);
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

