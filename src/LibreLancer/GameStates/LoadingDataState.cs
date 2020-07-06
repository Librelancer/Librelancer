// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;

namespace LibreLancer
{
    public class LoadingDataState : GameState
	{
		Texture2D splash;
        private bool invoked = false;
		public LoadingDataState(FreelancerGame g) : base(g)
		{
			splash = g.GameData.GetSplashScreen();
		}
        bool shadersCompiled = false;
        int xCnt = 0;
        public override void Draw(TimeSpan delta)
		{
            xCnt++;
			Game.Renderer2D.Start(Game.Width, Game.Height);
			Game.Renderer2D.DrawImageStretched(splash, new Rectangle(0, 0, Game.Width, Game.Height), Color4.White, true);
            DoFade(delta);
            Game.Renderer2D.Finish();
            if (!shadersCompiled && (xCnt >= 5))
            {
                Shaders.AllShaders.Compile();
                shadersCompiled = true;
            }
        }
		public override void Update(TimeSpan delta)
		{
            if (Game.InitialLoadComplete && !invoked)
            {
                invoked = true;
                Game.ResourceManager.Preload();
                Game.Fonts.LoadFontsFromGameData(Game.GameData);
                FadeOut(0.1, () =>
                {
                    if (Game.Config.CustomState != null)
                        Game.ChangeState(Game.Config.CustomState(Game));
                    else
                        Game.ChangeState(new LuaMenu(Game));
                });
            }
		}
    }
}

