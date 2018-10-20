// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and confiditons defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer
{
	public class LoadingDataState : GameState
	{
		Texture2D splash;
		public LoadingDataState(FreelancerGame g) : base(g)
		{
			splash = g.GameData.GetSplashScreen();
		}
		public override void Draw(TimeSpan delta)
		{
			Game.Renderer2D.Start(Game.Width, Game.Height);
			Game.Renderer2D.DrawImageStretched(splash, new Rectangle(0, 0, Game.Width, Game.Height), Color4.White, true);
			Game.Renderer2D.Finish();
		}
		public override void Update(TimeSpan delta)
		{
			if (Game.InitialLoadComplete)
			{
				Game.ResourceManager.Preload();
				Game.Fonts.LoadFonts();
				if (Game.Config.CustomState != null)
					Game.ChangeState(Game.Config.CustomState(Game));
				else
					Game.ChangeState(new LuaMenu(Game));
			}
		}
	}
}

