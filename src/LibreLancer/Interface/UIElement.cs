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

