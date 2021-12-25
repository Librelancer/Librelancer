// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using LibreLancer.GameData;
using LibreLancer.Interface;
using LiteNetLib;

namespace LibreLancer
{
    public class LuaMenu : GameState
    {
        UiContext ui;
        private UiWidget widget;
        IntroScene intro;
        Cutscene scene;
        Cursor cur;
        MenuAPI api;
        
        public LuaMenu(FreelancerGame g) : base(g)
        {
            
            api = new MenuAPI(this);
            ui = Game.Ui;
            ui.GameApi = api;
            ui.Visible = true;
            ui.OpenScene("mainmenu");
            g.GameData.PopulateCursors();
            g.CursorKind = CursorKind.None;
            intro = g.GameData.GetIntroScene();
            scene = new Cutscene(new ThnScriptContext(null), Game.GameData, Game.RenderContext.CurrentViewport, Game);
            scene.BeginScene(intro.Scripts);
            FLLog.Info("Thn", "Playing " + intro.ThnName);
            cur = g.ResourceManager.GetCursor("arrow");
            GC.Collect(); //crap
            g.Sound.PlayMusic(intro.Music);
            g.Keyboard.KeyDown += UiKeyDown;
            g.Keyboard.TextInput += UiTextInput;
#if DEBUG
            g.Keyboard.KeyDown += Keyboard_KeyDown;
#endif
            Game.Saves.Selected = -1;
            if (g.LoadTimer != null)
            {
                g.LoadTimer.Stop();
                FLLog.Info("Game", $"Initial load took {g.LoadTimer.Elapsed.TotalSeconds} seconds");
                g.LoadTimer = null;
            }
            FadeIn(0.1, 0.3);
        }

        private void UiTextInput(string text)
        {
            ui.OnTextEntry(text);
        }

        private void UiKeyDown(KeyEventArgs e)
        {
            ui.OnKeyDown(e.Key);
        }
        [MoonSharp.Interpreter.MoonSharpUserData]
        public class ServerList : ITableData
        {
            public List<LocalServerInfo> Servers = new List<LocalServerInfo>();
            public int Count => Servers.Count;
            public int Selected { get; set; } = -1;
            public string GetContentString(int row, string column)
            {
                if (row < 0 || row > Count || string.IsNullOrEmpty(column)) return null;
                switch (column.ToLowerInvariant())
                {
                    case "name":
                        return Servers[row].Name;
                    case "ip":
                        var addr = Servers[row].EndPoint.Address;
                        if (addr.IsIPv4MappedToIPv6)
                            return addr.MapToIPv4().ToString();
                        return addr.ToString();
                    case "visit":
                        return "NO";
                    case "ping":
                        return Servers[row].Ping.ToString();
                    case "players":
                        return $"{Servers[row].CurrentPlayers}/{Servers[row].MaxPlayers}";
                    case "version":
                        return Servers[row].DataVersion;
                    case "lan":
                        return "YES";
                    default:
                        return null;
                }
            }
            public string CurrentDescription()
            {
                if (Selected < 0 || Selected >= Count) return "";
                return Servers[Selected].Description;
            }
            public bool ValidSelection()
            {
                return (Selected >= 0 && Selected < Count);
            }
            public void Reset()
            {
                Selected = -1;
                Servers = new List<LocalServerInfo>();
            }
        }
        [MoonSharp.Interpreter.MoonSharpUserData]
        public class MenuAPI : UiApi
        {
            LuaMenu state;
            public MenuAPI(LuaMenu m)
            {
                state = m;
            }
            
            public GameSettings GetCurrentSettings() => state.Game.Config.Settings.MakeCopy();

            public void ApplySettings(GameSettings settings)
            {
                state.Game.Config.Settings = settings;
                state.Game.Config.Save();
            }

            public SaveGameFolder SaveGames() => state.Game.Saves;
            public void DeleteSelectedGame() => state.Game.Saves.TryDelete(state.Game.Saves.Selected);

            public void LoadSelectedGame()
            {
                state.FadeOut(0.2, () =>
                {
                    var embeddedServer = new EmbeddedServer(state.Game.GameData);
                    var session = new CGameSession(state.Game, embeddedServer);
                    embeddedServer.StartFromSave(state.Game.Saves.SelectedFile);
                    state.Game.ChangeState(new NetWaitState(session, state.Game));
                });
            }

            public override void NewGame()
            {
                state.FadeOut(0.2, () =>
                {
                    var embeddedServer = new EmbeddedServer(state.Game.GameData);
                    var session = new CGameSession(state.Game, embeddedServer);
                    embeddedServer.StartFromSave(state.Game.GameData.VFS.Resolve("EXE\\newplayer.fl"));
                    state.Game.ChangeState(new NetWaitState(session, state.Game));
                });
            }

            void ResolveNicknames(SelectableCharacter c)
            {
                c.Ship = state.Game.GameData.GetString(state.Game.GameData.GetShip(c.Ship).NameIds);
                c.Location = state.Game.GameData.GetSystem(c.Location).Name;
            }
            internal void _Update()
            {
                if (netClient == null) return;
                while (netClient.PollPacket(out var pkt))
                {
                    switch (pkt)
                    {
                        case OpenCharacterListPacket oclist:
                            FLLog.Info("Net", "Opening Character List");
                            this.cselInfo = oclist.Info;
                            foreach (var sc in oclist.Info.Characters)
                                ResolveNicknames(sc);
                            state.ui.Event("CharacterList");
                            break;
                        case AddCharacterPacket ac:
                            ResolveNicknames(ac.Character);
                            cselInfo.Characters.Add(ac.Character);
                            break;
                        case NewCharacterDBPacket ncdb:
                            state.ui.Event("OpenNewCharacter");
                            break;
                        default:
                            netSession.HandlePacket(pkt);
                            break;
                    }
                    
                }
            }
            private GameNetClient netClient;
            private CGameSession netSession;
            ServerList serverList = new ServerList();
            private CharacterSelectInfo cselInfo;

            public CharacterSelectInfo CharacterList() => cselInfo;
            public ServerList ServerList() => serverList;
            public void StartNetworking()
            {
                StopNetworking();
                netClient = new GameNetClient(state.Game);
                netSession = new CGameSession(state.Game, netClient);
                netClient.UUID = state.Game.Config.UUID.Value;
                netClient.ServerFound += info => serverList.Servers.Add(info);
                netClient.Disconnected += NetClientOnDisconnected;
                netClient.Start();
                RefreshServers();
            }

            public void RequestNewCharacter()
            {
                netSession.RpcServer.RequestCharacterDB();
            }

            public void LoadCharacter()
            {
                netSession.RpcServer.SelectCharacter(cselInfo.Selected);
                netClient.Disconnected += (str) => netSession.Disconnected();
                netClient.Disconnected -= NetClientOnDisconnected;
                netClient = null;
                state.FadeOut(0.2, () =>
                {
                    state.Game.ChangeState(new NetWaitState(netSession, state.Game));
                });
            }

            private int delIndex = -1;
            public void DeleteCharacter()
            {
                delIndex = cselInfo.Selected;
                netSession.RpcServer.DeleteCharacter(cselInfo.Selected).ContinueWith((t) =>
                {
                    if (t.Result) {
                        state.Game.QueueUIThread(() =>
                        {
                            cselInfo.Characters.RemoveAt(delIndex);
                            delIndex = -1;
                        });
                    }
                });
            }

            private void NetClientOnDisconnected(string obj)
            {
                netClient?.Shutdown();
                netClient = null;
                state.ui.Event("Disconnect");
            }

            public void RefreshServers()
            {
                serverList.Reset();
                netClient.DiscoverLocalPeers();
            }

            public void ConnectSelection()
            {
                if(serverList.Selected != -1)
                {
                    netClient.Connect(serverList.Servers[serverList.Selected].EndPoint);
                }
            }

            public bool ConnectAddress(string address) => netClient.Connect(address);


            public void NewCharacter(string name, int index)
            {
                FLLog.Info("Net", $"Requesting new char: `{name}`");
                netSession.RpcServer.CreateNewCharacter(name, index);
            }
            
            public void StopNetworking()
            {
                netClient?.Shutdown();
                netClient = null;
            }
            public override void Exit() => state.FadeOut(0.2, () => state.Game.Exit());
        }

        public override void Draw(double delta) 
        {
            RenderMaterial.VertexLighting = true;
            scene.Draw();
            ui.RenderWidget(delta);
            DoFade(delta);
            cur.Draw(Game.RenderContext.Renderer2D, Game.Mouse, Game.TotalTime);
        }


        int uframe = 0;
        bool newUI = false;
        public override void Update(double delta)
        {
            ui.Update(Game);
            Game.TextInputEnabled = ui.KeyboardGrabbed;
            scene.UpdateViewport(Game.RenderContext.CurrentViewport);
            scene.Update(delta);
            api._Update();
        }
#if DEBUG
        void LoadSpecific(int index)
        {
            intro = Game.GameData.GetIntroSceneSpecific(index);
            scene.Dispose();
            scene.BeginScene(intro.Scripts);
            scene.Update(1 / 60.0); //Do all the setup events - smoother entrance
            Game.Sound.PlayMusic(intro.Music);
        }
        
        void Keyboard_KeyDown(KeyEventArgs e)
        {
            if ((e.Modifiers & KeyModifiers.LeftControl) == KeyModifiers.LeftControl)
            {
                switch (e.Key)
                {
                    case Keys.D1:
                        LoadSpecific(0);
                        break;
                    case Keys.D2:
                        LoadSpecific(1);
                        break;
                    case Keys.D3:
                        LoadSpecific(2);
                        break;
                }
            }
        }
#endif

        public override void Exiting()
        {
            api.StopNetworking(); //Disconnect
        }

        public override void Unregister()
        {
            ui.Dispose();
            scene.Dispose();
            Game.Keyboard.KeyDown -= UiKeyDown;
            Game.Keyboard.TextInput -= UiTextInput;
#if DEBUG
            Game.Keyboard.KeyDown -= Keyboard_KeyDown;
#endif
        }
    }
}
