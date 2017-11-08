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
 * Portions created by the Initial Developer are Copyright (C) 2013-2017
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.GameData;
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
		Base currentBase;
		BaseRoom currentRoom;
		Cutscene scene;
		Hud hud;
		GameSession session;
		string baseId;
		Cursor cursor;
		public RoomGameplay(FreelancerGame g, GameSession session, string newBase, BaseRoom room = null) : base(g)
		{
			this.session = session;
			baseId = newBase;
			currentBase = g.GameData.GetBase(newBase);
			currentRoom = room ?? currentBase.StartRoom;
			SwitchToRoom();
			var tophotspots = new List<BaseHotspot>();
			foreach (var hp in currentRoom.Hotspots)
				if (TOP_IDS.Contains(hp.Name))
					tophotspots.Add(hp);
			hud = new Hud(g, tophotspots);
			hud.RoomMode();
			hud.OnEntered += Hud_OnTextEntry;
			hud.OnManeuverSelected += Hud_OnManeuverSelected;
			Game.Keyboard.TextInput += Game_TextInput;
			Game.Keyboard.KeyDown += Keyboard_KeyDown;
			cursor = Game.ResourceManager.GetCursor("arrow");
		}

		public override void Unregister()
		{
			Game.Keyboard.TextInput -= Game_TextInput;
			Game.Keyboard.KeyDown -= Keyboard_KeyDown;
			hud.Dispose();
		}

		bool Hud_OnManeuverSelected(string arg)
		{
			var hotspot = currentRoom.Hotspots.Find((obj) => obj.Name == arg);
			if (!hotspot.RoomIsVirtual) {
				var rm = currentBase.Rooms.Find((o) => o.Nickname == hotspot.Room);
				Game.ChangeState(new RoomGameplay(Game, session, baseId, rm));
			}
			return false;
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
			scene = new Cutscene(currentRoom.OpenScripts(), Game);
			if (currentRoom.Camera != null) scene.SetCamera(currentRoom.Camera);
			/*foreach (var npc in currentRoom.Npcs)
			{
				var obj = scene.Objects[npc.StandingPlace];
				var child = new GameObject();
				child.RenderComponent = new CharacterRenderer(
					Game.ResourceManager.GetDfm(npc.HeadMesh),
					Game.ResourceManager.GetDfm(npc.BodyMesh),
					Game.ResourceManager.GetDfm(npc.LeftHandMesh),
					Game.ResourceManager.GetDfm(npc.RightHandMesh)
				);
				child.Register(scene.Renderer, scene.World.Physics);
				child.Transform = Matrix4.CreateTranslation(0, 3, 0);
				obj.Object.Children.Add(child);
			}*/
			if (currentRoom.PlayerShipPlacement != null) {
				var shp = Game.GameData.GetShip(session.PlayerShip);
				var obj = new GameObject(shp.Drawable, Game.ResourceManager);
				obj.PhysicsComponent = null;
				var place = scene.Objects[currentRoom.PlayerShipPlacement];
				obj.Register(scene.Renderer, scene.World.Physics);
				obj.Transform = obj.GetHardpoint("HpMount").Transform.Inverted();
				place.Object.Children.Add(obj);
			}
		}

		public override void Update(TimeSpan delta)
		{
			if(scene != null)
				scene.Update(delta);
			hud.Update(delta, IdentityCamera.Instance);
		}

		public override void Draw(TimeSpan delta)
		{
			if(scene != null)
				scene.Draw();
			hud.Draw();
			Game.Renderer2D.Start(Game.Width, Game.Height);
			cursor.Draw(Game.Renderer2D, Game.Mouse);
			Game.Renderer2D.Finish();
		}
	}
}
