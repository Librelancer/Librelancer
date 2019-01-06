// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.GameData;
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
            FadeIn(0.1, 0.3);
        }

        class LuaAPI
        {
            LuaMenu state;
            public LuaAPI(LuaMenu m) => state = m;
            public void newgame() => state.ui.Leave(() =>
            {
                state.FadeOut(0.2, () => state.Game.ChangeState(new SpaceGameplay(state.Game, new GameSession(state.Game))));
            });
            public void loadgame() {}
            public void multiplayer() {}
            public void exit() => state.ui.Leave(() => state.FadeOut(0.2, () => state.Game.Exit()));
        }

        public override void Draw(TimeSpan delta) 
        {
            scene.Draw();
            ui.Draw(delta);
            Game.Renderer2D.Start(Game.Width, Game.Height);
            DoFade(delta);
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
            if (uframe < 8)
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
