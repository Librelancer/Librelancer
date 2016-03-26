using System;
using OpenTK;
using OpenTK.Graphics;

namespace LibreLancer
{
	public abstract class UIElement
	{
		public Vector2 UIPosition;
		public Vector2 UIScale;
		public string Tag;
		protected UIManager Manager;
		public UIAnimation Animation;

		public Vector2 Position {
			get {
				if (Animation != null && Animation.Running)
					return Animation.CurrentPosition;
				else
					return UIPosition;
			}
		}
		public Vector2 Scale {
			get {
				if (Animation != null &&
				    Animation.Running &&
				    Animation.CurrentScale != null)
					return Animation.CurrentScale.Value;
				else
					return UIScale;
			}
		}
		public UIElement (UIManager m)
		{
			Manager = m;
		}

		public abstract void DrawBase ();
		public abstract void DrawText ();

		public virtual void Update(TimeSpan time)
		{

		}

		protected Matrix4 GetWorld (Vector2 scale, Vector2 position)
		{
			return Matrix4.CreateScale (scale.X, scale.Y, 1f) * Matrix4.CreateTranslation (position.X, position.Y, 0f);
		}

		protected void DrawShadowedText (Font font, string text, float x, float y, Color4 c)
		{
			Manager.Game.Renderer2D.DrawString (font, text, x + 2, y + 2, Color4.Black);
			Manager.Game.Renderer2D.DrawString (font, text, x, y, c);
		}

		protected void DrawTextCentered (Font font, string text, Rectangle rect, Color4 c)
		{
			var size = Manager.Game.Renderer2D.MeasureString (font, text);
			var pos = new Vector2 (
				rect.X + (rect.Width / 2f - size.X / 2),
				rect.Y + (rect.Height / 2f - size.Y / 2)
			);			
			DrawShadowedText (font, text, pos.X, pos.Y,c);
		}
	}
}

