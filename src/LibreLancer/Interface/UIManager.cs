using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
namespace LibreLancer
{
	public class UIManager
	{
		public IDrawable MenuButton;
		public FreelancerGame Game;
		public Color4 TextColor = new Color4 (160, 196, 210, 255);
		public List<UIElement> Elements = new List<UIElement> ();
		public event Action<string> Clicked;
		Font buttonFont;
		float currentSize = -2f;
		public UIManager (FreelancerGame game)
		{
			Game = game;
		}
		public Font GetButtonFont(float sz)
		{
			if (currentSize != sz) {
				if (buttonFont != null)
					buttonFont.Dispose ();
				currentSize = sz;
				buttonFont = Font.FromSystemFont (Game.Renderer2D, "Agency FB", currentSize);
			}
			return buttonFont;
		}
		public void Draw()
		{
			Game.RenderState.DepthEnabled = false;
			foreach (var e in Elements)
				e.DrawBase ();
			Game.Renderer2D.Start (Game.Width, Game.Height);
			foreach (var e in Elements)
				e.DrawText ();
			Game.Renderer2D.Finish ();
		}

		public void OnClick(string tag)
		{
			if (Clicked != null)
				Clicked (tag);
		}

		public void Update(TimeSpan delta)
		{
			MenuButton.Update (IdentityCamera.Instance, delta);
			foreach (var elem in Elements)
				elem.Update (delta);
		}

		public Vector2 ScreenToPixel (float screenx, float screeny)
		{
			float distx = screenx * (Game.Width / 2);
			float x = (Game.Width / 2) + distx;

			float disty = screeny * (Game.Height / 2);
			float y = (Game.Height / 2) - disty;

			return new Vector2 (x, y);
		}
	}
}

