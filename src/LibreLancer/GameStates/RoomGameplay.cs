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
using LibreLancer.GameData;
namespace LibreLancer
{
	public class RoomGameplay : GameState
	{
		Base currentBase;
		BaseRoom currentRoom;
		Cutscene scene;
		Hud hud;
		GameSession session;
		public RoomGameplay(FreelancerGame g, GameSession session, string newBase) : base(g)
		{
			this.session = session;
			currentBase = g.GameData.GetBase(newBase);
			currentRoom = currentBase.StartRoom;
			SwitchToRoom();
			hud = new Hud(g);
			hud.RoomMode();
			hud.OnEntered += Hud_OnTextEntry;
			Game.Keyboard.TextInput += Game_TextInput;
			Game.Keyboard.KeyDown += Keyboard_KeyDown;
		}

		public override void Unregister()
		{
			Game.Keyboard.TextInput -= Game_TextInput;
			Game.Keyboard.KeyDown -= Keyboard_KeyDown;
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
		}
	}
}
