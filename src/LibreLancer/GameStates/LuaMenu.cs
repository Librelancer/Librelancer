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
 * Portions created by the Initial Developer are Copyright (C) 2013-2018
 * the Initial Developer. All Rights Reserved.
 */
using System;
using LibreLancer.GameData;
using Neo.IronLua;
namespace LibreLancer
{
    public class LuaMenu : GameState
    {
        XmlUIManager ui;
        IntroScene intro;
        Cutscene scene;
        Cursor cur;
        public LuaMenu(FreelancerGame g) : base(g)
        {
            ui = new XmlUIManager(g, "menu", new LuaAPI(this), g.GameData.GetInterfaceXml("mainmenu"));
            ui.OnConstruct();
            ui.Enter();
            g.GameData.PopulateCursors();
            g.CursorKind = CursorKind.None;
            intro = g.GameData.GetIntroScene();
            scene = new Cutscene(intro.Scripts, Game);
            scene.Update(TimeSpan.FromSeconds(1f / 60f)); //Do all the setup events - smoother entrance
            cur = g.ResourceManager.GetCursor("arrow");
            GC.Collect(); //crap
            g.Sound.PlayMusic(intro.Music);
            g.Keyboard.KeyDown += Keyboard_KeyDown;
        }

        class LuaAPI
        {
            LuaMenu state;
            public LuaAPI(LuaMenu m) => state = m;
            public void newgame() => state.ui.Leave(() =>
            {
                state.Game.ChangeState(new SpaceGameplay(state.Game, new GameSession(state.Game)));
            });
            public void loadgame() {}
            public void multiplayer() {}
            public void exit() => state.ui.Leave(() => state.Game.Exit());
        }

        public override void Draw(TimeSpan delta) 
        {
            scene.Draw();
            ui.Draw(delta);
            Game.Renderer2D.Start(Game.Width, Game.Height);
            cur.Draw(Game.Renderer2D, Game.Mouse);
            Game.Renderer2D.Finish();
        }


        int uframe = 0;
        bool newUI = false;
        public override void Update(TimeSpan delta)
        {
            if(newUI) {
                ui = new XmlUIManager(Game, "menu", new LuaAPI(this), Game.GameData.GetInterfaceXml("mainmenu"));
                ui.OnConstruct();
                ui.Enter();
                newUI = false;
            }
            if (uframe < 3)
            { //Allows animations to play correctly
                uframe++;
                ui.Update(TimeSpan.Zero);
            }
            else
            {
                ui.Update(delta);
                scene.Update(delta);
            }
        }

        void Keyboard_KeyDown(KeyEventArgs e)
        {
            if(e.Key == Keys.F5) {
                newUI = true;
            }
        }


        public override void Unregister()
        {
            ui.Dispose();
            scene.Dispose();
            Game.Keyboard.KeyDown -= Keyboard_KeyDown;
        }
    }
}
