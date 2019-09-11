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
        MenuAPI api;
        public LuaMenu(FreelancerGame g) : base(g)
        {
            api = new MenuAPI(this);
            ui = new XmlUIManager(g, g.GameData.GetInterfaceXml("mainmenu"), new LuaAPI("menu", api),
                new LuaAPI("options", new OptionsAPI(this)));
            ui.OnConstruct();
            ui.Enter();
            g.GameData.PopulateCursors();
            g.CursorKind = CursorKind.None;
            intro = g.GameData.GetIntroScene();
            scene = new Cutscene(intro.Scripts, Game);
            scene.Update(TimeSpan.FromSeconds(1f / 60f)); //Do all the setup events - smoother entrance
            FLLog.Info("Thn", "Playing " + intro.ThnName);
            cur = g.ResourceManager.GetCursor("arrow");
            GC.Collect(); //crap
            g.Sound.PlayMusic(intro.Music);
#if DEBUG
            g.Keyboard.KeyDown += Keyboard_KeyDown;
#endif
            FadeIn(0.1, 0.3);
        }

        class OptionsAPI
        {
            private LuaMenu state;
            public OptionsAPI(LuaMenu m) => state = m;
            public bool get_vsync() => state.Game.Config.VSync;
            public void set_vsync(bool value)
            {
                state.Game.Config.VSync = value;
                state.Game.Config.Save();
                state.Game.SetVSync(value);
            }
        }
        class MenuAPI
        {
            LuaMenu state;
            public MenuAPI(LuaMenu m) => state = m;
            public void newgame() => state.ui.Leave(() =>
            {
                state.FadeOut(0.2, () =>
                {
                    if (client != null) client.Dispose();
                    var session = new GameSession(state.Game);
                    session.LoadFromPath(state.Game.GameData.VFS.Resolve("EXE\\newplayer.fl"));
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
                client.Connect(serverList.Servers[serverList.Selection].EndPoint);
            }
            public void directconnect(string str)
            {
                if (!client.Connect(str))
                    state.ui.CallEvent("lookupfailed");
            }
            public void disconnect()
            {
                if (client != null) client.Dispose();
            }
            public void refreshservers()
            {
                serverList.Servers.Clear();
                serverList.Selection = -1;
                if (client != null) client.Dispose();
                client = new GameClient(state.Game, new GameSession(state.Game) { ExtraPackets = HandlePackets });
                client.Session.Client = client;
                client.ServerFound += Client_ServerFound;
                client.CharacterSelection += Client_CharacterSelection;
                client.Disconnected += Client_Disconnected;
                client.Start();
                client.UUID = state.Game.Config.UUID.Value;
                client.DiscoverLocalPeers();
            }

            void Client_Disconnected(string obj)
            {
                //Throw an error somehow
                state.ui.CallEvent("disconnected");
                refreshservers();
            }


            public void stopmp()
            {
                if (client != null) client.Dispose();
            }

            CharacterSelectInfo cinfo;
            void Client_CharacterSelection(CharacterSelectInfo obj)
            {
                cinfo = obj;
                foreach (var info in cinfo.Characters)
                    ResolveNicknames(info);
                state.ui.CallEvent("characterlist");
            }

            void ResolveNicknames(SelectableCharacter c)
            {
                c.Ship = state.Game.GameData.GetString(state.Game.GameData.GetShip(c.Ship).NameIds);
                c.Location = state.Game.GameData.GetSystem(c.Location).Name;
            }
            NewCharacterDBPacket characterDB;
            XmlUICharacterList charlist;
            public void opennewcharacter()
            {
                if (characterDB == null)
                {
                    client.SendReliablePacket(new CharacterListActionPacket()
                    {
                        Action = CharacterListAction.RequestCharacterDB
                    });
                }
                else
                    state.ui.CallEvent("newcharacter");
            }

            public void newcharacter(string name, int index)
            {
                client.SendReliablePacket(new CharacterListActionPacket()
                {
                    Action = CharacterListAction.CreateNewCharacter,
                    StringArg = name,
                    IntArg = index
                });
            }

            public string servernews()
            {
                if (cinfo != null) return cinfo.ServerNews;
                else return "";
            }
            public string servername()
            {
                if (cinfo != null) return cinfo.ServerName;
                else return "";
            }
            public string serverdescription()
            {
                if (cinfo != null) return cinfo.ServerDescription;
                else return "";
            }

            public void loadcharacter()
            {
                client.SendReliablePacket(new CharacterListActionPacket()
                {
                    Action = CharacterListAction.SelectCharacter,
                    IntArg = 0
                });
            }

            public void docharacterlist(XmlUICharacterList.CharacterListLua lua)
            {
                charlist = lua.CharList;
                charlist.Info = cinfo;
            }

            void HandlePackets(IPacket pkt)
            {
                switch(pkt)
                {
                    case NewCharacterDBPacket db:
                        characterDB = db;
                        state.ui.CallEvent("newcharacter");
                        break;
                    case AddCharacterPacket ac:
                        ResolveNicknames(ac.Character);
                        cinfo.Characters.Add(ac.Character);
                        break;
                }
            }

            void Client_ServerFound(LocalServerInfo obj)
            {
                serverList.Servers.Add(obj);
            }

            internal void _Dispose() //shouldn't be accessible from lua?
            {
                if(client != null) client.Dispose();
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
                api = new MenuAPI(this);
                ui = new XmlUIManager(Game, Game.GameData.GetInterfaceXml("mainmenu"), new LuaAPI("game", api),
                    new LuaAPI("options", new OptionsAPI(this)));
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

        public override void Exiting()
        {
            api._Dispose();
        }

        public override void Unregister()
        {
            ui.Dispose();
            scene.Dispose();
#if DEBUG
            Game.Keyboard.KeyDown -= Keyboard_KeyDown;
#endif
        }
    }
}
