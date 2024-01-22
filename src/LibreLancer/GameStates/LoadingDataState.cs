// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Graphics;
using LibreLancer.Interface;

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
        private bool uiLoaded = false;
        int xCnt = 0;
        public override void Draw(double delta)
		{
            xCnt++;
			Game.RenderContext.Renderer2D.DrawImageStretched(splash, new Rectangle(0, 0, Game.Width, Game.Height), Color4.White, true);
            DoFade(delta);
            if (!shadersCompiled && (xCnt >= 3))
            {
                Shaders.AllShaders.Compile(Game.RenderContext);
                shadersCompiled = true;
            }
            if (xCnt >= 3&& Game.InisLoaded && !uiLoaded)
            {
                Game.Fonts.LoadFontsFromGameData(Game.GameData);
                Game.Ui = new UiContext(Game);
                Game.Ui.LoadCode();
                FLLog.Info("UI", "Interface loaded");
                uiLoaded = true;
            }
        }
		public override void Update(double delta)
		{
            if (Game.InitialLoadComplete && shadersCompiled && uiLoaded && !invoked)
            {
                invoked = true;
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

