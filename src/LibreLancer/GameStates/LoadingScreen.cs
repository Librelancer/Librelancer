// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
    
using System;
using System.Collections.Generic;
using LibreLancer.Interface;
namespace LibreLancer
{
    //Not a full GameState object but close enough to one
    public class LoadingScreen
    {
        UiContext ui;
        private UiWidget widget;
        FreelancerGame game;
        IEnumerator<object> loader;
        public LoadingScreen(FreelancerGame game, IEnumerator<object> loader)
        {
            this.game = game;
            this.loader = loader;
            //ui = new UiContext(game, "loading.xml");
        }

        private int fCount = 0;
        private const int DELAY_FRAMES = 3;
        public bool Update(TimeSpan delta)
        {
            if(fCount > DELAY_FRAMES) ui.Update(game);
            double tick = game.TimerTick;
            while (game.TimerTick - tick < (1 / 30.0))
            {
                if (!loader.MoveNext())
                {
                    return true;
                }
            }
            fCount++;
            return false;
        }
        public void Draw(TimeSpan delta)
        {
            if(fCount > DELAY_FRAMES) ui.RenderWidget();
        }
    }
}
