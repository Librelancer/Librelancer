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
	public abstract class UIElement
	{
		public Vector2 UIPosition;
		public Vector2? OverridePosition;
		public Vector2 UIScale;
		public string Tag;
		protected UIManager Manager;
		public UIAnimation Animation;
		public bool Visible = true;
		public Vector2 Position
		{
			get
			{
				if (Animation != null && Animation.Running)
					return Animation.CurrentPosition;
				else if (OverridePosition != null)
					return OverridePosition.Value;
				else
					return UIPosition;
			}
		}
		public Vector2 Scale
		{
			get
			{
				if (Animation != null &&
					Animation.Running &&
					Animation.CurrentScale != null)
					return Animation.CurrentScale.Value;
				else
					return UIScale;
			}
		}
		public UIElement(UIManager m)
		{
			Manager = m;
		}

		public abstract void DrawBase();
		public abstract void DrawText();

		public void Reset()
		{
			OverridePosition = null;
		}

		public void Update(TimeSpan time)
		{
			if (Animation != null && Animation.Running) {
				Animation.Update(time.TotalSeconds);
				if (!Animation.Running && Animation.FinalPositionSet != null)
					OverridePosition = Animation.FinalPositionSet.Value;
			}
			else
				UpdateInternal(time);
		}

		protected virtual void UpdateInternal(TimeSpan time)
		{

		}

		protected Matrix4 GetWorld (Vector2 scale, Vector2 position)
		{
			return Matrix4.CreateScale (scale.X, scale.Y, 1f) * Matrix4.CreateTranslation (position.X, position.Y, 0f);
		}

		protected void DrawShadowedText (Font font, float size, string text, float x, float y, Color4 c)
		{
			Manager.Game.Renderer2D.DrawString (font, size, text, x + 2, y + 2, Color4.Black);
			Manager.Game.Renderer2D.DrawString (font, size, text, x, y, c);
		}

		protected void DrawTextCentered (Font font, float sz, string text, Rectangle rect, Color4 c)
		{
			var size = Manager.Game.Renderer2D.MeasureString (font, sz, text);
			var pos = new Vector2 (
				rect.X + (rect.Width / 2f - size.X / 2),
				rect.Y + (rect.Height / 2f - size.Y / 2)
			);			
			DrawShadowedText (font, sz, text, pos.X, pos.Y,c);
		}

		protected Rectangle FromScreenRect(float screenx, float screeny, float screenw, float screenh)
		{
			var p1 = IdentityCamera.Instance.ScreenToPixel(screenx, screeny);
			var p2 = IdentityCamera.Instance.ScreenToPixel(screenx + screenw, screeny - screenh);
			return new Rectangle(
				(int)(p1.X),
				(int)(p1.Y),
				(int)(p2.X - p1.X),
				(int)(p2.Y - p1.Y)
			);
		}

		public virtual bool TryGetHitRectangle(out Rectangle rect)
		{
			rect = new Rectangle(0, 0, 0, 0);
			return false;
		}

		public virtual void WasClicked()
		{
		}
	}
}

