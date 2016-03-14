using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;

namespace LibreLancer
{
	public class MainMenu : GameState
	{
		IDrawable menuButton;
		Vector2 scale = new Vector2 (2, 3f);
		Color4 TextColor = new Color4 (160, 196, 210, 255);
		Renderer2D uirender;
		Font aboutFont;
		Font buttonFont;
		string aboutText = "LIBRELANCER 0.1";
		List<UIButton> buttons = new List<UIButton> ();
		Texture2D logoOverlay;

		public MainMenu (FreelancerGame g) : base (g)
		{
			g.GameData.LoadInterfaceVms ();
			menuButton = g.GameData.GetMenuButton ();
			logoOverlay = g.GameData.GetFreelancerLogo ();

			uirender = new Renderer2D (g.RenderState);
			var textSize = GetTextSize (GetTextRectangle (0, 0).Height);
			aboutFont = Font.FromSystemFont (uirender, "Agency FB", 14);
			buttonFont = Font.FromSystemFont (uirender, "Agency FB", textSize);

			buttons.Add (new UIButton (new Vector2 (-0.65f, 0.40f), "NEW GAME", 0));
			buttons.Add (new UIButton (new Vector2 (-0.65f, 0.15f), "LOAD GAME", 0.1));
			buttons.Add (new UIButton (new Vector2 (-0.65f, -0.1f), "MULTIPLAYER", 0.2));
			buttons.Add (new UIButton (new Vector2 (-0.65f, -0.35f), "OPTIONS", 0.3));
			buttons.Add (new UIButton (new Vector2 (-0.65f, -0.6f), "EXIT", 0.4));
		}

		const double ANIMATION_LENGTH = 0.6;

		public override void Update (TimeSpan delta)
		{
			duration += delta.TotalSeconds;
			if (duration <= (ANIMATION_LENGTH + 0.4)) {
				foreach (var b in buttons) {
					b.Animate (duration);
				}
			}
			menuButton.Update (IdentityCamera.Instance, delta);
		}

		public override void Draw (TimeSpan delta)
		{
			//TODO: Draw background THN

			//Draw UI
			Game.RenderState.DepthEnabled = false;
			//UI Background
			uirender.Start (Game.Width, Game.Height);
			uirender.DrawImageStretched (logoOverlay, new Rectangle (0, 0, Game.Width, Game.Height), Color4.White, true);
			uirender.Finish ();
			//buttons
			foreach (var b in buttons) {
				menuButton.Draw (Game.RenderState,
					GetWorld (scale, b.CurrentPosition),
					Lighting.Empty
				);
			}
			//text
			uirender.Start (Game.Width, Game.Height);
			var pt = uirender.MeasureString (aboutFont, aboutText);
			DrawShadowedText (aboutFont, aboutText, Game.Width - pt.X - 15, Game.Height - pt.Y - 15);
			foreach (var b in buttons) {
				DrawTextCentered (b.Text, GetTextRectangle (b.CurrentPosition.X, b.CurrentPosition.Y));
			}
			uirender.Finish ();
		}

		class UIButton
		{
			public Vector2 FinalPosition;
			public Vector2 CurrentPosition;
			public string Text;
			public double AnimationStart;

			public UIButton (Vector2 pos, string text, double animStart)
			{
				FinalPosition = pos;
				Text = text;
				CurrentPosition = new Vector2 (-2, FinalPosition.Y);
				AnimationStart = animStart;
			}

			public void Animate (double duration)
			{
				if (duration > AnimationStart && (duration - AnimationStart) <= ANIMATION_LENGTH) {
					CurrentPosition.X = (float)CircEaseOut (
						duration - AnimationStart, -2,
						Math.Abs (FinalPosition.X - (-2)),
						ANIMATION_LENGTH
					);

				}
			}
		}

		double duration = 0;

		/// <summary>
		/// Easing equation function for a circular (sqrt(1-t^2)) easing out: 
		/// decelerating from zero velocity.
		/// </summary>
		/// <param name="t">Current time in seconds.</param>
		/// <param name="b">Starting value.</param>
		/// <param name="c">Change in value.</param>
		/// <param name="d">Duration of animation.</param>
		/// <returns>The correct value.</returns>
		public static double CircEaseOut (double t, double b, double c, double d)
		{
			return c * Math.Sqrt (1 - (t = t / d - 1) * t) + b;
		}

		void DrawShadowedText (Font font, string text, float x, float y)
		{
			uirender.DrawString (font, text, x + 2, y + 2, Color4.Black);
			uirender.DrawString (font, text, x, y, TextColor);
		}

		void DrawTextCentered (string text, Rectangle rect)
		{
			var size = uirender.MeasureString (buttonFont, text);
			var pos = new Vector2 (
				          rect.X + (rect.Width / 2f - size.X / 2),
				          rect.Y + (rect.Height / 2f - size.Y / 2)
			          );			
			DrawShadowedText (buttonFont, text, pos.X, pos.Y);
		}

		float GetTextSize (float px)
		{
			return (int)Math.Floor ((px * (72.0f / 96.0f)) - 14);
		}

		Rectangle GetTextRectangle (float screenx, float screeny)
		{
			var topleft = ScreenToPixel (screenx - 0.125f * scale.X, screeny + 0.022f * scale.Y);
			var bottomRight = ScreenToPixel (screenx + 0.125f * scale.X, screeny - 0.022f * scale.Y);
			var rect = new Rectangle (
				           (int)topleft.X,
				           (int)topleft.Y,
				           (int)(bottomRight.X - topleft.X),
				           (int)(bottomRight.Y - topleft.Y)
			           );
			return rect;
		}

		Vector2 ScreenToPixel (float screenx, float screeny)
		{
			float distx = screenx * (Game.Width / 2);
			float x = (Game.Width / 2) + distx;

			float disty = screeny * (Game.Height / 2);
			float y = (Game.Height / 2) - disty;

			return new Vector2 (x, y);
		}

		Matrix4 GetWorld (Vector2 scale, Vector2 position)
		{
			return Matrix4.CreateScale (scale.X, scale.Y, 1f) * Matrix4.CreateTranslation (position.X, position.Y, 0f);
		}
	}
}

