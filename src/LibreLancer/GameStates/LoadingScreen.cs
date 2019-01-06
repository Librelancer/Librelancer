// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
    
using System;
using System.Collections.Generic;
namespace LibreLancer
{
    //Not a full GameState object but close enough to one
    public class LoadingScreen
    {
        XmlUIManager manager;
        FreelancerGame game;
        IEnumerator<object> loader;
        public LoadingScreen(FreelancerGame game, IEnumerator<object> loader)
        {
            this.game = game;
            this.loader = loader;
            manager = new XmlUIManager(game, "game", null, game.GameData.GetInterfaceXml("loading"));
            manager.OnConstruct();
        }
        public bool Update(TimeSpan delta)
        {
            manager.Update(delta);
            double tick = game.TimerTick;
            while (game.TimerTick - tick < (1 / 30.0))
            {
                if (!loader.MoveNext())
                {
                    return true;
                }
            }
            return false;
        }
        public void Draw(TimeSpan delta)
        {
            manager.Draw(delta);
        }
    }
}
