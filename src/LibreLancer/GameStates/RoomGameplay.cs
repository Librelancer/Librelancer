// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Linq;
using LibreLancer.GameData;
using LibreLancer.Utf.Dfm;
using LibreLancer.Data.Missions;
using LibreLancer.Infocards;
using LibreLancer.Interface;

namespace LibreLancer
{
	public class RoomGameplay : GameState
	{
		static readonly string[] TOP_IDS = { //IDS that goes up the top?
			"IDS_HOTSPOT_DECK",
			"IDS_HOTSPOT_EQUIPMENTDEALER_ROOM",
			"IDS_HOTSPOT_SHIPDEALER_ROOM",
			"IDS_HOTSPOT_COMMODITYTRADER_ROOM",
			"IDS_HOTSPOT_BAR",
			"IDS_HOTSPOT_EXIT",
			"IDS_HOTSPOT_PLANETSCAPE"
		};
        const string LAUNCH_ACTION = "$LAUNCH";

		Base currentBase;        
        private StarSystem sys;
        BaseRoom currentRoom;
		Cutscene scene;
        private UiContext ui;
        
		CGameSession session;
		string baseId;
        string active;
		Cursor cursor;
		string virtualRoom;
        List<BaseHotspot> tophotspots;
        private ThnScript[] sceneScripts;
        private ThnScript waitingForFinish;
        private StoryCutsceneIni currentCutscene;
        private ScriptState currentState = ScriptState.None;
        private Infocard roomInfocard;
        enum ScriptState
        {
            None,
            Cutscene,
            Enter,
            Launch
        }
        
        public RoomGameplay(FreelancerGame g, CGameSession session, string newBase, BaseRoom room = null, string virtualRoom = null) : base(g)
        {
            //Load room data
            this.session = session;
			baseId = newBase;
			currentBase = g.GameData.GetBase(newBase);
			currentRoom = room ?? currentBase.StartRoom;
            currentRoom.InitForDisplay();
            var rm = virtualRoom ?? currentRoom.Nickname;
            this.virtualRoom = virtualRoom;
            //Find infocard
            sys = g.GameData.GetSystem(currentBase.System);
            var obj = sys.Objects.FirstOrDefault((o) =>
            {
                return o.Base?.Equals(newBase, StringComparison.OrdinalIgnoreCase) ?? false;
            });
            int ids = 0;
            if (obj?.IdsInfo.Length > 0) {
                ids = obj.IdsInfo[0];
            }
            roomInfocard = g.GameData.GetInfocard(ids, g.Fonts);
            if (g.GameData.GetRelatedInfocard(ids, g.Fonts, out var ic2))
            {
                roomInfocard.Nodes.Add(new RichTextParagraphNode());
                roomInfocard.Nodes.AddRange(ic2.Nodes);
            }
            //Create user interface
            tophotspots = new List<BaseHotspot>();
            foreach (var hp in currentRoom.Hotspots)
                if (TOP_IDS.Contains(hp.Name))
                    tophotspots.Add(hp);
            SetActiveHotspot(rm);
            Game.Keyboard.TextInput += Game_TextInput;
            Game.Keyboard.KeyDown += Keyboard_KeyDown;
            cursor = Game.ResourceManager.GetCursor("arrow");
            ui = Game.Ui;
            ui.GameApi = new BaseUiApi(this);
            ui.OpenScene("baseside");
            //Set up THN
            SwitchToRoom(room == null);
            FadeIn(0.8, 1.7);
        }
        void SetActiveHotspot(string rm)
        {
            foreach (var hp in tophotspots) {
                if (hp.SetVirtualRoom == rm) {
                    active = hp.Name;
                    return;
                }
            }
            foreach (var hp in tophotspots) {
                if (hp.Room == rm) {
                    active = hp.Name;
                    return;
                }
            }
        }

        [MoonSharp.Interpreter.MoonSharpUserData]
        public class BaseUiApi : UiApi
        {
            RoomGameplay g;
            public BaseUiApi(RoomGameplay g) => this.g = g;
            public bool IsMultiplayer() => false;
            public void HotspotPressed(string item) => g.Hud_OnManeuverSelected(item);
            public string ActiveNavbarButton() => g.active;

            public Infocard CurrentInfocard() => g.roomInfocard;

            public void PopulateNavmap(Navmap navmap)
            {
                navmap.PopulateIcons(g.ui, g.sys);
            }
            public NavbarButtonInfo[] GetNavbarButtons()
            {
                var buttons = new NavbarButtonInfo[g.tophotspots.Count];
                for (int i = 0; i < buttons.Length; i++)
                {
                    buttons[i] = new NavbarButtonInfo(
                        g.tophotspots[i].Name, 
                        g.tophotspots[i].SetVirtualRoom ?? g.tophotspots[i].Room
                    );
                }

                return buttons;
            }
            public NavbarButtonInfo[] GetActionButtons()
            {
                var actions = new List<NavbarButtonInfo>();
                if (string.IsNullOrEmpty(g.virtualRoom) &&
                    (g.currentRoom.Nickname.Equals("cityscape", StringComparison.OrdinalIgnoreCase) ||
                     g.currentRoom.Nickname.Equals("deck", StringComparison.OrdinalIgnoreCase) ||
                     g.currentRoom.Nickname.Equals("planetscape", StringComparison.OrdinalIgnoreCase)))
                {
                    actions.Add(new NavbarButtonInfo(LAUNCH_ACTION, "IDS_HOTSPOT_LAUNCH"));
                }
                return actions.ToArray();
            }
            public void TextEntered(string text)
            {
                g.Hud_OnTextEntry(text);
            }
        }
		public override void Unregister()
		{
			Game.Keyboard.TextInput -= Game_TextInput;
			Game.Keyboard.KeyDown -= Keyboard_KeyDown;
			scene.Dispose();
		}
        
        List<StoryCutsceneIni> processedCutscenes = new List<StoryCutsceneIni>();
        void ProcessCutscenes()
        {
            foreach (var ct in session.ActiveCutscenes)
            {
                if (processedCutscenes.Contains(ct)) continue;
                if (ct.Encounters.Count != 1) continue;
                if (!ct.Encounters[0].Autoplay) continue;
                var n = ct.Encounters[0].Location;
                if (n[0].Equals(currentBase.Nickname, StringComparison.OrdinalIgnoreCase) &&
                    n[1].Equals(currentRoom.Nickname, StringComparison.OrdinalIgnoreCase))
                {
                    processedCutscenes.Add(ct);
                    ui.Visible = false;
                    var script = new ThnScript(session.Game.GameData.ResolveDataPath(ct.Encounters[0].Action));
                    currentCutscene = ct;
                    RoomDoSceneScript(script, ScriptState.Cutscene);
                }
            }
        }

        private bool didLaunch = false;
        public void Launch()
        {
            if (!string.IsNullOrEmpty(currentRoom.LaunchScript))
            {
                RoomDoSceneScript(new ThnScript(currentRoom.LaunchScript), ScriptState.Launch);
            }
            else
            {
                SendLaunch();
            }
        }

        void SendLaunch()
        {
            if (didLaunch) return;
            session.Launch();
            didLaunch = true;
        }

        void Hud_OnManeuverSelected(string arg)
        {
            if (arg == active) return;
            Game.QueueUIThread(() => //Fixes stack trace
            {
                if(arg == LAUNCH_ACTION)
                {
                    Launch();
                    return;
                }

                var hotspot = currentRoom.Hotspots.Find((obj) => obj.Name == arg);
                switch (hotspot.Behavior)
                {
                    case "ExitDoor":
                        var rm = currentBase.Rooms.Find((o) => o.Nickname == hotspot.Room);
                        FadeOut(0.6, () => Game.ChangeState(new RoomGameplay(Game, session, baseId, rm, hotspot.SetVirtualRoom)));
                        break;
                    case "VirtualRoom":
                        FadeOut(0.6, () => Game.ChangeState(new RoomGameplay(Game, session, baseId, currentRoom, hotspot.Room)));
                        break;
                }
            });
		}

		void Keyboard_KeyDown(KeyEventArgs e)
		{
			if (ui.KeyboardGrabbed)
			{
				ui.OnKeyDown(e.Key);
			}
			else
			{
                if (e.Key == Keys.Escape)
                {
                    switch (currentState)
                    {
                        case ScriptState.Launch:
                            SendLaunch();
                            break;
                        case ScriptState.Enter:
                            FadeOut(0.25, () =>
                            {
                                RoomDoSceneScript(null, ScriptState.None);
                                FadeIn(0.2, 0.25);
                            });
                            break;
                    }
                }
				if (e.Key == Keys.Enter)
                {
                    ui.ChatboxEvent();
                }
			}
		}

		void Game_TextInput(string text)
		{
            ui.OnTextEntry(text);
		}
		void Hud_OnTextEntry(string obj)
		{
            session.ProcessConsoleCommand(obj);
		}

        private GameObject playerShip;
		void SwitchToRoom(bool dolanding)
        {
            session.RoomEntered(virtualRoom ?? currentRoom.Nickname, currentBase.Nickname);
			if (currentRoom.Music == null)
			{
				Game.Sound.StopMusic();
			}
			else
			{
				Game.Sound.PlayMusic(currentRoom.Music, currentRoom.MusicOneShot);
			}
            var shp = Game.GameData.GetShip(session.PlayerShip);
            playerShip = new GameObject(shp.ModelFile.LoadFile(Game.ResourceManager), Game.ResourceManager); 
            playerShip.PhysicsComponent = null;
            var ctx = new ThnScriptContext(currentRoom.OpenSet());
            ctx.PlayerShip = playerShip;
            if(currentBase.TerrainTiny != null) ctx.Substitutions.Add("$terrain_tiny", currentBase.TerrainTiny);
            if(currentBase.TerrainSml != null) ctx.Substitutions.Add("$terrain_sml", currentBase.TerrainSml);
            if(currentBase.TerrainMdm != null) ctx.Substitutions.Add("$terrain_mdm", currentBase.TerrainMdm);
            if(currentBase.TerrainLrg != null) ctx.Substitutions.Add("$terrain_lrg", currentBase.TerrainLrg);
            if(currentBase.TerrainDyna1 != null) ctx.Substitutions.Add("$terrain_dyna_01", currentBase.TerrainDyna1);
            if(currentBase.TerrainDyna2 != null) ctx.Substitutions.Add("$terrain_dyna_02", currentBase.TerrainDyna2);
            scene = new Cutscene(ctx, Game.GameData, Game.Viewport, Game);
            scene.ScriptFinished += SceneOnScriptFinished;
            sceneScripts = currentRoom.OpenScene().ToArray();
            if (dolanding && !string.IsNullOrEmpty(currentRoom.LandScript))
            {
                RoomDoSceneScript(new ThnScript(currentRoom.LandScript), ScriptState.Enter);
            } else if (!string.IsNullOrEmpty(currentRoom.StartScript))
            {
                RoomDoSceneScript(new ThnScript(currentRoom.StartScript), ScriptState.Enter);
            }
            else
            {
                RoomDoSceneScript(null, ScriptState.None);
            }
        }

        private void SceneOnScriptFinished(ThnScript obj)
        {
            if (waitingForFinish != null && obj == waitingForFinish)
            {
                if (currentCutscene != null)
                {
                    FadeOut(0.25, () =>
                    {
                        RoomDoSceneScript(null, ScriptState.None);
                        ui.Visible = true;
                        FLLog.Info("Thn", "Finished cutscene");
                        session.FinishCutscene(currentCutscene);
                        currentCutscene = null;
                        FadeIn(0.2,0.25);
                    });
                }
                else if (currentState == ScriptState.Launch)
                {
                    SendLaunch();
                } 
                else
                {
                    currentState = ScriptState.None;
                    SetRoomCameraAndShip();
                    animatingLetterbox = true;
                }
            }
        }

        private double letterboxAmount = 1;
        private bool animatingLetterbox = false;

        IEnumerable<ThnScript> Scripts(params IEnumerable<ThnScript>[] objects)
        {
            foreach (var obj in objects)
            {
                if (obj != null)
                {
                    foreach (var scn in obj)
                        if (scn != null)
                            yield return scn;
                }
            }
        }

        void SetRoomCameraAndShip()
        {
            if (currentRoom.Camera != null)
                scene.SetCamera(currentRoom.Camera);
            ThnObject shipMarker = scene.GetObject(currentRoom.PlayerShipPlacement);
            if (shipMarker != null) {
                if(playerShip.HardpointExists("HpMount"))
                {
                    Matrix4x4.Invert(playerShip.GetHardpoint("HpMount").Transform, out var tr);
                    playerShip.Transform = tr;
                }
                else
                {
                    playerShip.Transform = Matrix4x4.Identity;
                }
                shipMarker.Object.Children.Add(playerShip);
            }
        }

        private bool firstFrame = false; //Stops a desync in scene starting
        void RoomDoSceneScript(ThnScript sc, ScriptState state)
        {
            firstFrame = true;
            currentState = state;
            if (sc == null) currentState = ScriptState.None;
            waitingForFinish = sc;
            scene.BeginScene(Scripts(sceneScripts, new[] { sc }));
            var ships = currentBase.SoldShips.Select(x => x.Package.Ship).ToArray();
            for(int i = 0; (i < ships.Length && i < currentRoom.ForSaleShipPlacements.Count); i++)
            {
                ThnObject marker = scene.GetObject(currentRoom.ForSaleShipPlacements[i]);
                if(marker == null)
                {
                    FLLog.Error("Base", "Couldn't display " + ships[i] + " on " + currentRoom.ForSaleShipPlacements[i]);
                    continue;
                }
                var toSellShip = Game.GameData.GetShip(ships[i]);
                //Set up object
                var obj = new GameObject(toSellShip.ModelFile.LoadFile(Game.ResourceManager), Game.ResourceManager) { Parent = marker.Object };
                obj.PhysicsComponent = null;
                marker.Object.Children.Add(obj);
                if(obj.HardpointExists("HpMount"))
                {
                    Matrix4x4.Invert(obj.GetHardpoint("HpMount").Transform, out var tr);
                    obj.Transform = tr;
                }
            }
            if (sc == null) {
                SetRoomCameraAndShip();
                letterboxAmount = -1;
                ui.Visible = true;
            }
            else {
                ui.Visible = false;
                letterboxAmount = 1;
            }
        }

		public override void Update(TimeSpan delta)
        {
            session.Update();
            ProcessCutscenes();
            if (scene != null) {
                scene.UpdateViewport(Game.Viewport);
                scene.Update(firstFrame ? TimeSpan.Zero : delta);
            }
            firstFrame = false;
            ui.Update(Game);
            if (ui.KeyboardGrabbed)
                Game.EnableTextInput();
            else
                Game.DisableTextInput();
        }


        
		public override void Draw(TimeSpan delta)
        {
            RenderMaterial.VertexLighting = true;
            if (scene != null)
				scene.Draw();
            ui.RenderWidget(delta);
			Game.Renderer2D.Start(Game.Width, Game.Height);
            DoFade(delta);
            #if DEBUG
            Game.Renderer2D.DrawString("Arial", 15, "Room: " + currentRoom.Nickname + "\n" + "Virtual: " +
                (virtualRoom ?? "NONE"), new Vector2(5, 5), Color4.White);
            #endif
            if (letterboxAmount > 0)
            {
                var pct = Cutscene.LETTERBOX_HEIGHT * (float) letterboxAmount;
                int h = (int) (Game.Height * pct);
                Game.Renderer2D.FillRectangle(new Rectangle(0, 0, Game.Width, h), Color4.Black);
                Game.Renderer2D.FillRectangle(new Rectangle(0, Game.Height - h, Game.Width, h), Color4.Black);
            }
            if (animatingLetterbox)
            {
                letterboxAmount -= delta.TotalSeconds * 3;
                if (letterboxAmount < 0)
                {
                    letterboxAmount = -1;
                    animatingLetterbox = false;
                    ui.Visible = true;
                }
            }
            if(ui.Visible) cursor.Draw(Game.Renderer2D, Game.Mouse);
            Game.Renderer2D.Finish();
        }

        public override void Exiting()
        {
            session.OnExit();
        }
    }
}
