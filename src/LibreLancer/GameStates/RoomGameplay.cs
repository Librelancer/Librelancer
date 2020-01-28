// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.GameData;
using LibreLancer.Utf.Dfm;
using LibreLancer.Data.Missions;
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
		BaseRoom currentRoom;
		Cutscene scene;
        private UiContext ui;
        private UiWidget widget;
        
		GameSession session;
		string baseId;
        string active;
		Cursor cursor;
		string virtualRoom;
        List<BaseHotspot> tophotspots;
        public RoomGameplay(FreelancerGame g, GameSession session, string newBase, BaseRoom room = null, string virtualRoom = null) : base(g)
		{
            this.session = session;
			baseId = newBase;
			currentBase = g.GameData.GetBase(newBase);
			currentRoom = room ?? currentBase.StartRoom;
            currentRoom.InitForDisplay();
			SwitchToRoom();
			tophotspots = new List<BaseHotspot>();
			foreach (var hp in currentRoom.Hotspots)
				if (TOP_IDS.Contains(hp.Name))
					tophotspots.Add(hp);
            var rm = virtualRoom ?? currentRoom.Nickname;
            SetActiveHotspot(rm);
            this.virtualRoom = virtualRoom;
            ui = new UiContext(Game);
            ui.GameApi = new BaseUiApi(this);
            widget = ui.CreateAll("baseside.xml");
            Game.Keyboard.TextInput += Game_TextInput;
			Game.Keyboard.KeyDown += Keyboard_KeyDown;
			cursor = Game.ResourceManager.GetCursor("arrow");
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

        class BaseUiApi : UiApi
        {
            RoomGameplay g;
            public BaseUiApi(RoomGameplay g) => this.g = g;
            public bool IsMultiplayer() => false;
            public void HotspotPressed(string item) => g.Hud_OnManeuverSelected(item);
            public string ActiveNavbarButton() => g.active;

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
        }
		public override void Unregister()
		{
			Game.Keyboard.TextInput -= Game_TextInput;
			Game.Keyboard.KeyDown -= Keyboard_KeyDown;
            ui.Unhook();
			scene.Dispose();
		}
        
        List<StoryCutsceneIni> processedCutscenes = new List<StoryCutsceneIni>();

        bool GotCutscene()
        {
            foreach (var ct in session.ActiveCutscenes)
            {
                if (ct.Encounters.Count != 1) continue;
                if (!ct.Encounters[0].Autoplay) continue;
                var n = ct.Encounters[0].Location;
                if (n[0].Equals(currentBase.Nickname, StringComparison.OrdinalIgnoreCase) &&
                    n[1].Equals(currentRoom.Nickname, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
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
                    widget.Visible = false;
                    var script = new ThnScript(session.Game.GameData.ResolveDataPath(ct.Encounters[0].Action));
                    scene.RunScript(script, () =>
                    {
                        widget.Visible = true;
                        session.ActiveCutscenes.Remove(ct);
                    });
                }
            }

        }

        void Hud_OnManeuverSelected(string arg)
        {
            if (arg == active) return;
            Game.QueueUIThread(() => //Fixes stack trace
            {
                if(arg == LAUNCH_ACTION) {
                    FLLog.Info("Base", "Launch!");
                    session.LaunchFrom(baseId);
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
				if (e.Key == Keys.L)
				{
					Game.Screenshots.TakeScreenshot();
				}
				if (e.Key == Keys.B)
				{
					if (currentRoom.Nickname.ToLowerInvariant() != "shipdealer")
					{
						var rm = currentBase.Rooms.Find((o) => o.Nickname.ToLowerInvariant() == "shipdealer");
						Game.ChangeState(new RoomGameplay(Game, session, baseId, rm));
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
			//hud.OnTextEntry(text);
		}
		void Hud_OnTextEntry(string obj)
		{
            if(obj == "launch") {
                scene.RunScript(new ThnScript(currentRoom.LaunchScript));
            }
            session.ProcessConsoleCommand(obj);
		}

		void SwitchToRoom()
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
            shp.LoadResources();
            var PlayerShip = new GameObject(shp.Drawable, Game.ResourceManager);
            PlayerShip.PhysicsComponent = null;
            var ctx = new ThnScriptContext(currentRoom.OpenScripts(!GotCutscene()));
            ctx.PlayerShip = PlayerShip;
            if(currentBase.TerrainTiny != null) ctx.Substitutions.Add("$terrain_tiny", currentBase.TerrainTiny);
            if(currentBase.TerrainSml != null) ctx.Substitutions.Add("$terrain_sml", currentBase.TerrainSml);
            if(currentBase.TerrainMdm != null) ctx.Substitutions.Add("$terrain_mdm", currentBase.TerrainMdm);
            if(currentBase.TerrainLrg != null) ctx.Substitutions.Add("$terrain_lrg", currentBase.TerrainLrg);
            if(currentBase.TerrainDyna1 != null) ctx.Substitutions.Add("$terrain_dyna_01", currentBase.TerrainDyna1);
            if(currentBase.TerrainDyna2 != null) ctx.Substitutions.Add("$terrain_dyna_02", currentBase.TerrainDyna2);
            scene = new Cutscene(ctx, Game.GameData, Game.Viewport, Game);
			if (currentRoom.Camera != null) scene.SetCamera(currentRoom.Camera);
			/*foreach (var npc in currentRoom.Npcs)
			{
				var obj = scene.Objects[npc.StandingPlace];
				var child = new GameObject();
				child.RenderComponent = new CharacterRenderer(
                    (DfmFile)Game.ResourceManager.GetDrawable(npc.HeadMesh),
                    (DfmFile)Game.ResourceManager.GetDrawable(npc.BodyMesh),
					(DfmFile)Game.ResourceManager.GetDrawable(npc.LeftHandMesh),
                    (DfmFile)Game.ResourceManager.GetDrawable(npc.RightHandMesh)
				);
                child.Register(scene.World.Physics);
				child.Transform = Matrix4.CreateTranslation(0, 3, 0);
				obj.Object.Children.Add(child);
			}*/
            var ships = currentBase.SoldShips.Select(x => x.Package.Ship).ToArray();
            for(int i = 0; (i < ships.Length && i < currentRoom.ForSaleShipPlacements.Count); i++)
            {
                ThnObject marker;
                if(!scene.Objects.TryGetValue(currentRoom.ForSaleShipPlacements[i],out marker))
                {
                    FLLog.Error("Base", "Couldn't display " + ships[i] + " on " + currentRoom.ForSaleShipPlacements[i]);
                    continue;
                }
                var toSellShip = Game.GameData.GetShip(ships[i]);
                toSellShip.LoadResources();
                //Set up player object + camera
                var obj = new GameObject(toSellShip.Drawable, Game.ResourceManager) { Parent = marker.Object };
                obj.PhysicsComponent = null;
                marker.Object.Children.Add(obj);
                if(obj.HardpointExists("HpMount"))
                {
                    obj.Transform = obj.GetHardpoint("HpMount").Transform.Inverted();
                }
            }
        }

		public override void Update(TimeSpan delta)
		{
            ProcessCutscenes();
			if(scene != null)
				scene.Update(delta);
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
            ui.RenderWidget();
			Game.Renderer2D.Start(Game.Width, Game.Height);
            DoFade(delta);
            Game.Renderer2D.DrawString("Arial", 15, "Room: " + currentRoom.Nickname + "\n" + "Virtual: " +
                (virtualRoom ?? "NONE"), new Vector2(5, 5), Color4.White);
            cursor.Draw(Game.Renderer2D, Game.Mouse);
            Game.Renderer2D.Finish();
		}

        public override void Exiting()
        {
            session.OnExit();
        }
    }
}
