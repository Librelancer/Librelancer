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
        LuaAPI api;
        public LuaMenu(FreelancerGame g) : base(g)
        {
            api = new LuaAPI(this);
            ui = new XmlUIManager(g, "menu", api, g.GameData.GetInterfaceXml("mainmenu"));
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
#if DEBUG
            g.Keyboard.KeyDown += Keyboard_KeyDown;
#endif
            FadeIn(0.1, 0.3);
        }

        class LuaAPI
        {
            LuaMenu state;
            public LuaAPI(LuaMenu m) => state = m;
            public void newgame() => state.ui.Leave(() =>
            {
                state.FadeOut(0.2, () =>
                {
                    var session = new GameSession(state.Game);
                    session.LoadFromPath(Data.VFS.GetPath("EXE\\newplayer.fl"));
                    session.Start();
                });
            });
            public void loadgame() {}
            GameClient client;
            XmlUIServerList serverList;
            public void doserverlist(XmlUIServerList.ServerListAPI slist)
            {
                serverList = slist.Srv;
                refreshservers();
            }
            public bool canconnect() => serverList.Selection >= 0;
            public void connectserver()
            {

            }
            public void refreshservers()
            {
                serverList.Servers.Clear();
                serverList.Selection = -1;
                if (client != null) client.Dispose();
                client = new GameClient(state.Game);
                client.ServerFound += Client_ServerFound;
                client.Start();
                client.UUID = state.Game.Config.UUID.Value;
                client.DiscoverLocalPeers();
            }
            void Client_ServerFound(LocalServerInfo obj)
            {
                serverList.Servers.Add(obj);
            }

            internal void _Dispose() //shouldn't be accessible from lua?
            {
                if (client != null) {
                    client.Dispose();
                }
            }

            public void exit() => state.ui.Leave(() => state.FadeOut(0.2, () => state.Game.Exit()));
        }

        public override void Draw(TimeSpan delta) 
        {
            RenderMaterial.VertexLighting = true;
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
#if DEBUG
            if(newUI) {
                api._Dispose();
                api = new LuaAPI(this);
                ui = new XmlUIManager(Game, "menu", api, Game.GameData.GetInterfaceXml("mainmenu"));
                ui.OnConstruct();
                ui.Enter();
                newUI = false;
            }
#endif
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
#if DEBUG
        void Keyboard_KeyDown(KeyEventArgs e)
        {
            if(e.Key == Keys.F5) {
                newUI = true;
            }
        }
#endif

        public override void Unregister()
        {
            ui.Dispose();
            scene.Dispose();
            api._Dispose();
#if DEBUG
            Game.Keyboard.KeyDown -= Keyboard_KeyDown;
#endif
        }
    }
}
