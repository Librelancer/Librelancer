// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Runtime.InteropServices;
using LibreLancer.Client;
using LibreLancer.Data.GameData;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Input;
using LibreLancer.Interface;
using LibreLancer.Items;
using LibreLancer.Net;
using LibreLancer.Net.Protocol;
using LibreLancer.Render;
using LibreLancer.Resources;
using LibreLancer.Thn;
using LiteNetLib;
using WattleScript.Interpreter;
using DisconnectReason = LibreLancer.Net.DisconnectReason;

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
            TryRunScript(intro.Scripts);
            FLLog.Info("Thn", "Playing " + intro.ThnName);
            cur = g.ResourceManager.GetCursor("arrow");
            GC.Collect(); //crap
            g.Sound.PlayMusic(intro.Music, 0);
            g.Keyboard.KeyDown += UiKeyDown;
            g.Keyboard.TextInput += UiTextInput;
#if DEBUG
            g.Keyboard.KeyDown += Keyboard_KeyDown;
#endif
            g.Keyboard.KeyUp += Keyboard_OnKeyUp;
            g.Mouse.MouseUp += Mouse_MouseUp;
            Game.Saves.Selected = -1;
            if (g.LoadTimer != null)
            {
                g.LoadTimer.Stop();
                FLLog.Info("Game", $"Initial load took {g.LoadTimer.Elapsed.TotalSeconds} seconds");
                g.LoadTimer = null;
            }
            // Set low latency GC mode only once everything has been loaded in
            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
            FadeIn(0.1, 0.3);
        }
        void TryRunScript(List<ResolvedThn> thnScripts)
        {
            var intro = new List<ThnScript>();
            scene = new Cutscene(new ThnScriptContext(null), Game.GameData, Game.ResourceManager, Game.Sound, Game.RenderContext.CurrentViewport, Game);
            foreach (var s in thnScripts)
            {
                #if !DEBUG
                try
                {
                    intro.Add(s.LoadScript());
                }
                catch (Exception e)
                {
                    FLLog.Error("Thn", $"Error loading script {s.SourcePath}: {e.Message}\n{e.StackTrace}");
                    scene = null;
                    return;
                }
                #else
                intro.Add(s.LoadScript());
                #endif
            }
            scene.BeginScene(intro);
        }

        public override void OnSettingsChanged()
        {
            if (scene?.Renderer != null)
                scene.Renderer.Settings = Game.Config.Settings;
        }

        private void Mouse_MouseUp(MouseEventArgs e)
        {
            if (e.Buttons != MouseButtons.Left && KeyCaptureContext.Capturing(keyCapture))
            {
                keyCapture.Set(UserInput.FromMouse(e.Buttons));
            }
        }

        private KeyCaptureContext keyCapture;
        private void Keyboard_OnKeyUp(KeyEventArgs e)
        {
            if (KeyCaptureContext.Capturing(keyCapture))
            {
                if (e.Key != Keys.Escape &&
                    e.Key != Keys.F1)
                {
                    keyCapture.Set(UserInput.FromKey(e.Modifiers, e.Key));
                }
                else
                {
                    keyCapture.Cancel();
                }
            }
        }


        private void UiTextInput(string text)
        {
            if(!KeyCaptureContext.Capturing(keyCapture))
                ui.OnTextEntry(text);
        }

        private void UiKeyDown(KeyEventArgs e)
        {
            if (!KeyCaptureContext.Capturing(keyCapture))
            {
                if (e.Key == Keys.Escape)
                    ui.OnEscapePressed();
                ui.OnKeyDown(e.Key, (e.Modifiers & KeyModifiers.Control) != 0);
            }
        }

        [WattleScriptUserData]
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
        [WattleScript.Interpreter.WattleScriptUserData]
        public class MenuAPI : UiApi
        {
            LuaMenu state;
            public MenuAPI(LuaMenu m)
            {
                state = m;
            }

            public KeyMapTable GetKeyMap()
            {
                var table = new KeyMapTable(state.Game.InputMap, state.Game.GameData.Items.Ini.Infocards);
                table.OnCaptureInput += (k) =>
                {
                    state.keyCapture = k;
                };
                return table;
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
                    var embeddedServer = new EmbeddedServer(state.Game.GameData, state.Game.ResourceManager,
                        state.Game.GetSaveFolder());
                    var session = new CGameSession(state.Game, embeddedServer);
                    embeddedServer.StartFromSave(state.Game.Saves.SelectedFile, File.ReadAllBytes(state.Game.Saves.SelectedFile));
                    state.Game.ChangeState(new NetWaitState(session, state.Game));
                });
            }

            public override void NewGame()
            {
                state.FadeOut(0.2, () =>
                {
                    var embeddedServer = new EmbeddedServer(state.Game.GameData, state.Game.ResourceManager,
                        state.Game.GetSaveFolder());
                    var session = new CGameSession(state.Game, embeddedServer);
                    embeddedServer.StartFromSave("EXE\\newplayer.fl", state.Game.GameData.VFS.ReadAllBytes("EXE\\newplayer.fl"));
                    state.Game.ChangeState(new NetWaitState(session, state.Game));
                });
            }

            private UiNewCharacter[] newCharacters;

            public UiNewCharacter[] GetNewCharacters() => newCharacters;

            void ResolveNicknames(SelectableCharacter c)
            {
                c.Ship = state.Game.GameData.GetString(state.Game.GameData.Items.Ships.Get(c.Ship).NameIds);
                c.Location = state.Game.GameData.GetString(state.Game.GameData.Items.Systems.Get(c.Location).IdsName);
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
                        {
                            newCharacters = new UiNewCharacter[ncdb.Factions.Count];
                            for (int i = 0; i < ncdb.Factions.Count; i++)
                            {
                                var package = ncdb.Packages.First(x =>
                                    x.Nickname.Equals(ncdb.Factions[i].Package, StringComparison.OrdinalIgnoreCase));
                                var ship = state.Game.GameData.Items.Ships.Get(package.Ship);
                                ship.ModelFile.LoadFile(state.Game.ResourceManager);
                                var loc = state.Game.GameData.GetString(state.Game.GameData.Items.Bases.Get(ncdb.Factions[i].Base).IdsName);
                                newCharacters[i] = new UiNewCharacter()
                                {
                                    Money = package.Money,
                                    StridDesc = package.StridDesc,
                                    StridName = package.StridName,
                                    ShipName = state.Game.GameData.GetString(ship.NameIds),
                                    ShipModel = ship.ModelFile.ModelFile,
                                    Location = loc
                                };
                            }
                            state.ui.Event("OpenNewCharacter");
                            break;
                        }
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
                netClient.AuthenticationRequired += NetClientOnAuthenticationRequired;
                netClient.Start();
                RefreshServers();
            }

            private void NetClientOnAuthenticationRequired(bool retry)
            {
                if(retry) state.ui.Event("IncorrectPassword");
                else state.ui.Event("Login");
            }

            public void Login(string username, string password)
            {
                netClient?.Login(username, password);
            }

            public void RequestNewCharacter()
            {
                netSession.RpcServer.RequestCharacterDB();
            }

            public void LoadCharacter()
            {
                netSession.RpcServer.SelectCharacter(cselInfo.Selected).ContinueWith(x => state.Game.QueueUIThread(() =>
                {
                    if (x.Result)
                    {
                        state.FadeOut(0.2, () =>
                        {
                            netClient.Disconnected += (reason) => netSession.Disconnected();
                            netClient.Disconnected -= NetClientOnDisconnected;
                            netClient = null;
                            state.Game.ChangeState(new NetWaitState(netSession, state.Game));
                        });
                    }
                    else
                    {
                        state.ui.Event("SelectCharFailure");
                    }
                }));


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

            private void NetClientOnDisconnected(DisconnectReason reason)
            {
                netClient?.Shutdown();
                netClient = null;
                state.ui.Event("Disconnect", reason.ToString());
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

            public void ConnectAddress(string address) => netClient.Connect(address);


            public void NewCharacter(string name, int index, Closure onError)
            {
                FLLog.Info("Net", $"Requesting new char: `{name}`");
                netSession.RpcServer.CreateNewCharacter(name, index).ContinueWith((task) => {
                    if(!task.Result) state.Game.QueueUIThread(() => onError.Call());
                });
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
            scene?.Draw(delta, Game.Width, Game.Height);
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
            scene?.UpdateViewport(Game.RenderContext.CurrentViewport, (float)Game.Width / Game.Height);
            scene?.Update(delta);
            api._Update();
        }
#if DEBUG
        void LoadSpecific(int index)
        {
            intro = Game.GameData.GetIntroSceneSpecific(index);
            scene?.Dispose();
            TryRunScript(intro.Scripts);
            scene?.Update(1 / 60.0); //Do all the setup events - smoother entrance
            Game.Sound.PlayMusic(intro.Music, 0);
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

        protected override void OnUnload()
        {
            scene?.Dispose();
            Game.Keyboard.KeyDown -= UiKeyDown;
            Game.Keyboard.TextInput -= UiTextInput;
#if DEBUG
            Game.Keyboard.KeyDown -= Keyboard_KeyDown;
#endif
            Game.Keyboard.KeyUp -= Keyboard_OnKeyUp;
            Game.Mouse.MouseUp -= Mouse_MouseUp;
        }
    }
}
