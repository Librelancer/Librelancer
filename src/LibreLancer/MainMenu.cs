using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;

namespace LibreLancer
{
	public class MainMenu : GameState
	{
		Texture2D logoOverlay;
		UIManager manager;
		string lastTag = null;
		public MainMenu (FreelancerGame g) : base (g)
		{
			g.GameData.LoadInterfaceVms ();
			logoOverlay = g.GameData.GetFreelancerLogo ();

			manager = new UIManager (g);
			manager.MenuButton = g.GameData.GetMenuButton ();
			manager.Elements.Add (new UIMenuButton (manager, new Vector2 (-0.65f, 0.40f), "NEW GAME", "new"));
			manager.Elements.Add (new UIMenuButton (manager, new Vector2 (-0.65f, 0.15f), "LOAD GAME"));
			manager.Elements.Add (new UIMenuButton (manager, new Vector2 (-0.65f, -0.1f), "MULTIPLAYER"));
			manager.Elements.Add (new UIMenuButton (manager, new Vector2 (-0.65f, -0.35f), "OPTIONS"));
			manager.Elements.Add (new UIMenuButton (manager, new Vector2 (-0.65f, -0.6f), "EXIT", "exit"));
			manager.Clicked += (tag) => lastTag = tag;
		}

		const double ANIMATION_LENGTH = 0.6;

		public override void Update (TimeSpan delta)
		{
			manager.Update (delta);
			if (lastTag == "new") {
				Game.ChangeState (new DemoSystemView (Game));
			}
			if (lastTag == "exit") {
				Game.Exit ();
			}
			lastTag = null;
		}

		public override void Draw (TimeSpan delta)
		{
			//TODO: Draw background THN

			//UI Background
			Game.Renderer2D.Start (Game.Width, Game.Height);
			Game.Renderer2D.DrawImageStretched (logoOverlay, new Rectangle (0, 0, Game.Width, Game.Height), Color4.White, true);
			Game.Renderer2D.Finish ();
			//buttons
			manager.Draw();
		}
	}
}

