using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
namespace LibreLancer
{
	public class MainMenu : GameState
	{
		IDrawable menuButton;
		IdentityCamera camera;
		Lighting lights;
		Vector2 scale = new Vector2 (2, 3f);
		Color4 TextColor = new Color4 (160, 196, 210, 255);
		Renderer2D uirender;
		Font aboutFont;
		Font buttonFont;
		string aboutText = "LIBRELANCER 0.1";
		List<UIButton> buttons = new List<UIButton>();
		Texture2D logoOverlay;
		public MainMenu (FreelancerGame g) : base(g)
		{
			g.GameData.LoadInterfaceVms ();
			menuButton = g.GameData.GetMenuButton ();
			logoOverlay = g.GameData.GetFreelancerLogo ();
			lights = new Lighting (); // no lighting
			uirender = new Renderer2D(g.RenderState);
			var textSize = GetTextSize (GetTextRectangle (0, 0).Height);
			aboutFont = Font.FromSystemFont (uirender, "Agency FB", 14);
			buttonFont = Font.FromSystemFont (uirender, "Agency FB", textSize);

			buttons.Add (new UIButton (new Vector2 (-0.65f, 0.40f), "NEW GAME"));
			buttons.Add (new UIButton (new Vector2 (-0.65f, 0.15f), "LOAD GAME"));
			buttons.Add (new UIButton (new Vector2 (-0.65f, -0.1f), "MULTIPLAYER"));
			buttons.Add (new UIButton (new Vector2 (-0.65f, -0.35f), "OPTIONS"));
			buttons.Add (new UIButton (new Vector2 (-0.65f, -0.6f), "EXIT"));
		}
		public override void Update (TimeSpan delta)
		{
			menuButton.Update (new IdentityCamera (), delta);
		}
		public override void Draw (TimeSpan delta)
		{
			//TODO: Draw background THN

			//Draw UI
			Game.RenderState.DepthEnabled = false;
			//UI Background
			uirender.Start(Game.Width, Game.Height);
			uirender.DrawImageStretched (logoOverlay, new Rectangle (0, 0, Game.Width, Game.Height), Color4.White, true);
			uirender.Finish ();
			//buttons
			foreach (var b in buttons) {
				menuButton.Draw (Game.RenderState,
					GetWorld (scale, b.Position),
					lights
				);
			}
			//text
			uirender.Start(Game.Width,Game.Height);
			var pt = uirender.MeasureString (aboutFont, aboutText);
			DrawShadowedText (aboutFont, aboutText, Game.Width - pt.X - 15, Game.Height - pt.Y - 15);
			foreach (var b in buttons) {
				DrawTextCentered (b.Text, GetTextRectangle (b.Position.X, b.Position.Y));
			}
			uirender.Finish ();
		}
		class UIButton {
			public Vector2 Position;
			public string Text;
			public UIButton(Vector2 pos, string text)
			{
				Position = pos;
				Text = text;
			}
		}
		void DrawShadowedText(Font font, string text, float x, float y)
		{
			uirender.DrawString (font, text, x + 2, y + 2, Color4.Black);
			uirender.DrawString (font, text, x, y, TextColor);
		}
		void DrawTextCentered(string text, Rectangle rect)
		{
			var size = uirender.MeasureString (buttonFont, text);
			var pos = new Vector2 (
				          rect.X + (rect.Width / 2f - size.X / 2),
				          rect.Y + (rect.Height / 2f - size.Y / 2)
			          );			
			DrawShadowedText (buttonFont, text, pos.X, pos.Y);
		}
		float GetTextSize(float px)
		{
			return (int)Math.Floor((px * (72.0f / 96.0f)) - 14);
		}
		Rectangle GetTextRectangle(float screenx, float screeny)
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
		Vector2 ScreenToPixel(float screenx, float screeny)
		{
			float distx = screenx * (Game.Width / 2);
			float x = (Game.Width / 2) + distx;

			float disty = screeny * (Game.Height / 2);
			float y = (Game.Height / 2) - disty;

			return new Vector2 (x, y);
		}
		Matrix4 GetWorld(Vector2 scale, Vector2 position)
		{
			return Matrix4.CreateScale (scale.X, scale.Y, 1f) * Matrix4.CreateTranslation (position.X, position.Y, 0f);
		}
		class IdentityCamera : ICamera
		{
			public Matrix4 ViewProjection {
				get {
					return Matrix4.Identity;
				}
			}
		}
	}
}

