// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.GameData;
using LibreLancer.Utf.Dfm;
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
		ScriptedHud hud;
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
			SwitchToRoom();
			tophotspots = new List<BaseHotspot>();
			foreach (var hp in currentRoom.Hotspots)
				if (TOP_IDS.Contains(hp.Name))
					tophotspots.Add(hp);
            var rm = virtualRoom ?? currentRoom.Nickname;
            SetActiveHotspot(rm);
            hud = new ScriptedHud(new LuaAPI(this), false, Game);
            hud.OnEntered += Hud_OnTextEntry;
            hud.Init();
			this.virtualRoom = virtualRoom;
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

        class LuaAPI
        {
            RoomGameplay g;
            public LuaAPI(RoomGameplay g) => this.g = g;
            public bool multiplayer() => false;
            public void navclick(string item) => g.Hud_OnManeuverSelected(item);
            public string activebutton() => g.active;
            public Neo.IronLua.LuaTable buttons()
            {
                var list = new Neo.IronLua.LuaTable();
                var icons = g.Game.GameData.GetBaseNavbarIcons();
                foreach (var btn in g.tophotspots) {
                    var mn = (dynamic)(new Neo.IronLua.LuaTable());
                    mn.action = btn.Name;
                    string hack = null;
                    if (!icons.ContainsKey(btn.SetVirtualRoom ?? btn.Room))
                        hack = "Cityscape"; //HACK: This probably means FL doesn't determine icons based on room name
                    var icn = icons[hack ?? btn.SetVirtualRoom ?? btn.Room];
                    mn.model = "//" + icn;
                    g.hud.UI.TableInsert(list, mn);
                }
                return list;
            }
            public Neo.IronLua.LuaTable actions()
            {
                var list = new Neo.IronLua.LuaTable();
                var icons = g.Game.GameData.GetBaseNavbarIcons();
                if (g.virtualRoom == null &&
                    g.currentRoom.Nickname.Equals("cityscape",StringComparison.OrdinalIgnoreCase) ||
                    g.currentRoom.Nickname.Equals("deck",StringComparison.OrdinalIgnoreCase) ||
                    g.currentRoom.Nickname.Equals("planetscape", StringComparison.OrdinalIgnoreCase)) 
                {
                    var mn = (dynamic)(new Neo.IronLua.LuaTable());
                    mn.action = LAUNCH_ACTION;
                    mn.model = "//" + icons["IDS_HOTSPOT_LAUNCH"];
                    g.hud.UI.TableInsert(list, mn);
                }
                return list;
            }
        }
		public override void Unregister()
		{
			Game.Keyboard.TextInput -= Game_TextInput;
			Game.Keyboard.KeyDown -= Keyboard_KeyDown;
			hud.Dispose();
			scene.Dispose();
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
			if (hud.TextEntry)
			{
				hud.TextEntryKeyPress(e.Key);
				if (hud.TextEntry == false) Game.DisableTextInput();
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
					hud.TextEntry = true;
					Game.EnableTextInput();
				}
			}
		}

		void Game_TextInput(string text)
		{
			hud.OnTextEntry(text);
		}
		void Hud_OnTextEntry(string obj)
		{
            if(obj == "launch") {
                scene.RunScript(new ThnScript(currentRoom.LaunchScript));
            } else if (obj == "reload") {
                hud = new ScriptedHud(new LuaAPI(this), false, Game);
                hud.OnEntered += Hud_OnTextEntry;
                hud.Init();
            }
            session.ProcessConsoleCommand(obj);
		}

		void SwitchToRoom()
		{
			if (currentRoom.Music == null)
			{
				Game.Sound.StopMusic();
			}
			else
			{
				Game.Sound.PlayMusic(currentRoom.Music);
			}
            var shp = Game.GameData.GetShip(session.PlayerShip);
            var PlayerShip = new GameObject(shp.Drawable, Game.ResourceManager);
            PlayerShip.PhysicsComponent = null;

            scene = new Cutscene(currentRoom.OpenScripts(), Game, PlayerShip);
			if (currentRoom.Camera != null) scene.SetCamera(currentRoom.Camera);
			foreach (var npc in currentRoom.Npcs)
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
			}
		}

		public override void Update(TimeSpan delta)
		{
			if(scene != null)
				scene.Update(delta);
            hud.Update(delta);
        }

       
		public override void Draw(TimeSpan delta)
		{
            RenderMaterial.VertexLighting = true;
            if (scene != null)
				scene.Draw();
            hud.Draw(delta);
			Game.Renderer2D.Start(Game.Width, Game.Height);
            DoFade(delta);
            Game.Renderer2D.DrawString(hud.UI.Font, 15, "Room: " + currentRoom.Nickname + "\n" + "Virtual: " +
                (virtualRoom ?? "NONE"), new Vector2(5, 5), Color4.White);
            cursor.Draw(Game.Renderer2D, Game.Mouse);
            Game.Renderer2D.Finish();
		}
	}
}
