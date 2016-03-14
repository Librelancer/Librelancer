using System;
using OpenTK;
namespace LibreLancer
{
	public class UIMenuButton : UIElement
	{
		Vector2 scale = new Vector2 (3, 2);
		public string Title = "";
		public Vector2 Position = Vector2.Zero;

		public UIMenuButton (UIManager manager) : base(manager)
		{
		}

		public override void DrawBase ()
		{
			Manager.MenuButton.Draw (
				Manager.Game.RenderState,
				GetWorld (scale, Position),
				Lighting.Empty
			);
		}
		public override void DrawText()
		{
			
		}

		float GetTextSize (float px)
		{
			return (int)Math.Floor ((px * (72.0f / 96.0f)) - 14);
		}

		Rectangle GetTextRectangle (float screenx, float screeny)
		{
			var topleft = Manager.ScreenToPixel (screenx - 0.125f * scale.X, screeny + 0.022f * scale.Y);
			var bottomRight = Manager.ScreenToPixel (screenx + 0.125f * scale.X, screeny - 0.022f * scale.Y);
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

