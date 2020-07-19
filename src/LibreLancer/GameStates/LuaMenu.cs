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
            ui = new UiContext(g, "mainmenu.xml");
            ui.GameApi = api;
            ui.Start();
            g.GameData.PopulateCursors();
            g.CursorKind = CursorKind.None;
            intro = g.GameData.GetIntroScene();
            scene = new Cutscene(new ThnScriptContext(intro.Scripts), Game.GameData, Game.Viewport, Game);
            scene.Update(TimeSpan.FromSeconds(1f / 60f)); //Do all the setup events - smoother entrance
            FLLog.Info("Thn", "Playing " + intro.ThnName);
            cur = g.ResourceManager.GetCursor("arrow");
            GC.Collect(); //crap
            g.Sound.PlayMusic(intro.Music);
            g.Keyboard.KeyDown += UiKeyDown;
            g.Keyboard.TextInput += UiTextInput;
#if DEBUG
            g.Keyboard.KeyDown += Keyboard_KeyDown;
#endif
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
            public MenuAPI(LuaMenu m) => state = m;
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
                        case CharacterListActionResponsePacket cresp:
                            switch (cresp.Action)
                            {
                                case CharacterListAction.DeleteCharacter:
                                    if (cresp.Status == CharacterListStatus.OK)
                                    {
                                        cselInfo.Characters.RemoveAt(delIndex);
                                        delIndex = -1;
                                    }
                                    break;
                            }
                            break;
                    }
                }
            }
            private GameNetClient netClient;
            ServerList serverList = new ServerList();
            private CharacterSelectInfo cselInfo;

            public CharacterSelectInfo CharacterList() => cselInfo;
            public ServerList ServerList() => serverList;
            public void StartNetworking()
            {
                StopNetworking();
                netClient = new GameNetClient(state.Game);
                netClient.UUID = state.Game.Config.UUID.Value;
                netClient.ServerFound += info => serverList.Servers.Add(info);
                netClient.Disconnected += NetClientOnDisconnected;
                netClient.Start();
                RefreshServers();
            }

            public void RequestNewCharacter()
            {
                netClient.SendPacket(new CharacterListActionPacket()
                {
                    Action = CharacterListAction.RequestCharacterDB
                }, PacketDeliveryMethod.ReliableOrdered);
            }

            public void LoadCharacter()
            {
                netClient.SendPacket(new CharacterListActionPacket()
                {
                    Action = CharacterListAction.SelectCharacter,
                    IntArg = cselInfo.Selected
                }, PacketDeliveryMethod.ReliableOrdered);
                var session = new CGameSession(state.Game, netClient);
                netClient.Disconnected += (str) => session.Disconnected();
                netClient.Disconnected -= NetClientOnDisconnected;
                netClient = null;
                state.FadeOut(0.2, () =>
                {
                    state.Game.ChangeState(new NetWaitState(session, state.Game));
                });
            }

            private int delIndex = -1;
            public void DeleteCharacter()
            {
                delIndex = cselInfo.Selected;
                netClient.SendPacket(new CharacterListActionPacket()
                {
                    Action = CharacterListAction.DeleteCharacter,
                    IntArg = cselInfo.Selected
                }, PacketDeliveryMethod.ReliableOrdered);
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
                netClient.Connect(serverList.Servers[serverList.Selected].EndPoint);
            }

            public void NewCharacter(string name, int index)
            {
                netClient.SendPacket(new CharacterListActionPacket()
                {
                    Action = CharacterListAction.CreateNewCharacter,
                    StringArg =  name,
                    IntArg = index
                }, PacketDeliveryMethod.ReliableOrdered);
            }
            
            public void StopNetworking()
            {
                netClient?.Shutdown();
                netClient = null;
            }
            public override void Exit() => state.FadeOut(0.2, () => state.Game.Exit());
        }

        public override void Draw(TimeSpan delta) 
        {
            RenderMaterial.VertexLighting = true;
            scene.Draw();
            ui.RenderWidget();
            Game.Renderer2D.Start(Game.Width, Game.Height);
            DoFade(delta);
            cur.Draw(Game.Renderer2D, Game.Mouse);
            Game.Renderer2D.Finish();
        }


        int uframe = 0;
        bool newUI = false;
        public override void Update(TimeSpan delta)
        {
            ui.Update(Game);
            Game.TextInputEnabled = ui.KeyboardGrabbed;
            scene.Update(delta);
            api._Update();
        }
#if DEBUG
        void LoadSpecific(int index)
        {
            intro = Game.GameData.GetIntroSceneSpecific(index);
            scene.Dispose();
            scene = new Cutscene(new ThnScriptContext(intro.Scripts), Game.GameData, Game.Viewport, Game);
            scene.Update(TimeSpan.FromSeconds(1f / 60f)); //Do all the setup events - smoother entrance
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
