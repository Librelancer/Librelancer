using System;
using OpenTK;
namespace LibreLancer
{
	public abstract class UIElement
	{
		public Vector2 Position;
		public Vector2 Scale;
		protected UIManager Manager;

		public UIElement (UIManager m)
		{
			Manager = m;
		}

		public abstract void DrawBase ();
		public abstract void DrawText ();

		protected Matrix4 GetWorld (Vector2 scale, Vector2 position)
		{
			return Matrix4.CreateScale (scale.X, scale.Y, 1f) * Matrix4.CreateTranslation (position.X, position.Y, 0f);
		}
	}
}

