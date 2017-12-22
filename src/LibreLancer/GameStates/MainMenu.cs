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
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.Collections.Generic;
using LibreLancer.GameData;
namespace LibreLancer
{
	public class MainMenu : GameState
	{
		Texture2D logoOverlay;
		UIManager manager;
		string lastTag = null;
		const double FLYIN_LENGTH = 0.6;
		IntroScene intro;
		Cutscene scene;
		Cursor cur;
		public MainMenu(FreelancerGame g) : base(g)
		{
			g.GameData.LoadHardcodedFiles();
			g.GameData.PopulateCursors();
			g.MouseVisible = false;

			logoOverlay = g.GameData.GetFreelancerLogo();

			g.WillClose += G_WillClose;
			manager = new UIManager(g);
			manager.MenuButton = g.GameData.GetMenuButton();
			manager.Clicked += (tag) => lastTag = tag;
			ConstructMainMenu();

			intro = g.GameData.GetIntroScene();
			scene = new Cutscene(intro.Scripts, Game);
			scene.Update(TimeSpan.FromSeconds(1f / 60f)); //Do all the setup events - smoother entrance
			GC.Collect(); //crap
			g.Sound.PlayMusic(intro.Music);

#if DEBUG
			g.Keyboard.KeyDown += Keyboard_KeyDown;
#endif
			cur = g.ResourceManager.GetCursor("arrow");
			GC.Collect(); //GC before showing
		}
#if DEBUG
		void Keyboard_KeyDown(KeyEventArgs e)
		{
			if (e.Key >= Keys.D1 && e.Key <= Keys.D9)
			{
				var i = (int)e.Key - (int)Keys.D1;
				var r = Game.GameData.GetIntroSceneSpecific(i);
				if (r == null) return;
				intro = r;
				scene = new Cutscene(intro.Scripts, Game);
				scene.Update(TimeSpan.FromSeconds(1f / 60f)); //Do all the setup events - smoother entrance
				Game.Sound.PlayMusic(intro.Music);
				GC.Collect(); //crap
			}

		}

#endif
		public override void Unregister()
		{
#if DEBUG
			Game.Keyboard.KeyDown -= Keyboard_KeyDown;
#endif
			Game.WillClose -= G_WillClose;
			manager.Dispose();
		}


		void G_WillClose()
		{
			if (netClient != null) netClient.Dispose();
		}

		int frames = 0;
		int dframes = 0;
		public override void Update (TimeSpan delta)
		{
			//Don't want the big lag at the start
			if (frames == 0)
			{
				frames = 1;
				return;
			}
			scene.Update(delta);
			manager.Update (delta);
			if (lastTag == "gameplay")
			{
				manager.PlaySound("ui_motion_swish");
				manager.FlyOutAll(FLYIN_LENGTH, 0.05);
				manager.AnimationComplete += () =>
				{
					manager.Dispose();
					Game.ChangeState(new SpaceGameplay(Game, new GameSession(Game)));
				};
			}
			if (lastTag == "multiplayer")
			{
				ConstructLanInternetDialog(); //Don't bother with ESRB notice just skip to LAN/INTERNET
			}
			if (lastTag == "dlg_cancel")
			{
				manager.Dialog = null;
			}
			if (lastTag == "system") {
				manager.PlaySound("ui_motion_swish");
				manager.FlyOutAll(FLYIN_LENGTH, 0.05);
				manager.AnimationComplete += () =>
				{
					manager.Dispose();
					Game.ChangeState(new DemoSystemView(Game));
				};
			}
			if (lastTag == "options") {
				manager.PlaySound("ui_motion_swish");
				manager.FlyOutAll(FLYIN_LENGTH, 0.05);
				manager.AnimationComplete += ConstructOptions;
			}
			if (lastTag == "opt_mainmenu") {
				manager.PlaySound("ui_motion_swish");
				manager.FlyOutAll(FLYIN_LENGTH, 0.05);
				manager.AnimationComplete += ConstructMainMenu;
			}
			if (lastTag == "srvlst_refresh") {
				connectButton.Tag = null;
				serverList.Servers.Clear();
				netClient.DiscoverLocalPeers();
				if (internetServers)
					netClient.DiscoverGlobalPeers();
			}
			if (lastTag == "srvlst_connect") {
				netClient.Connect(selectedInfo.EndPoint);
			}
			if (lastTag == "srvlst_mainmenu") {
				netClient.Disconnected -= ServerList_Disconnected;
				netClient.Dispose();
				netClient = null;
				manager.PlaySound("ui_motion_swish");
				manager.FlyOutAll(FLYIN_LENGTH, 0.05);
				manager.AnimationComplete += ConstructMainMenu;
			}
			if (lastTag == "csel_mainmenu")
			{
				netClient.Disconnected -= CharSelect_Disconnected;
				netClient.Dispose();
				netClient = null;
				manager.PlaySound("ui_motion_swish");
				manager.FlyOutAll(FLYIN_LENGTH, 0.05);
				manager.AnimationComplete += ConstructMainMenu;
			}
			if (lastTag == "csel_servlist")
			{
				netClient.Disconnected -= CharSelect_Disconnected;
				netClient.Stop();
				netClient.Start();
				manager.PlaySound("ui_motion_swish");
				manager.FlyOutAll(FLYIN_LENGTH, 0.05);
				manager.AnimationComplete += ConstructServerList;
			}
			if (lastTag == "exit") {
				manager.PlaySound("ui_motion_swish");
				manager.FlyOutAll(FLYIN_LENGTH, 0.05);
				manager.AnimationComplete += () =>
				{
					manager.Dispose();
					if (netClient != null)
					{
						netClient.Dispose();
					}
					Game.Exit();
				};
			}
			lastTag = null;
		}


		const int LANORINTERNET_INFOCARD = 393712;
		void ConstructLanInternetDialog()
		{
			var dlg = new List<UIElement>();
			dlg.Add(new UIBackgroundElement(manager) { FillColor = new Color4(0, 0, 0, 0.25f) });
			dlg.Add(new UIMessageBox(manager, LANORINTERNET_INFOCARD));
			var x = new UIXButton(manager, 0.64f, 0.26f, 2, 2.9f);
			x.Clicked += () =>
			{
				manager.Dialog = null;
				manager.PlaySound("ui_motion_swish");
				manager.FlyOutAll(FLYIN_LENGTH, 0.05);
				manager.AnimationComplete += ConstructServerList;
			};
			dlg.Add(x);

			manager.Dialog = dlg;

		}

		bool internetServers = false;
		GameClient netClient;
		UIServerList serverList;
		UIServerDescription serverDescription;
		UIMenuButton connectButton;
		LocalServerInfo selectedInfo;
		CharacterSelectInfo csel;

		void ServerList_Selected(LibreLancer.LocalServerInfo obj)
		{
			selectedInfo = obj;
			serverDescription.Description = obj.Description;
			connectButton.Tag = "srvlst_connect";
		}


		void ConstructCharacterSelect()
		{
			manager.Elements.Clear();
			manager.AnimationComplete -= ConstructCharacterSelect;
			netClient.Disconnected += CharSelect_Disconnected;
			Vector2 buttonScale = new Vector2(1.87f, 2.5f);
			manager.Elements.Add(new UIMenuButton(manager, new Vector2(-0.70f, 0.42f), "NEW CHARACTER", "opt_general") { UIScale = buttonScale });
			manager.Elements.Add(new UIMenuButton(manager, new Vector2(-0.70f, 0.24f), "LOAD CHARACTER", "opt_controls") { UIScale = buttonScale });
			manager.Elements.Add(new UIMenuButton(manager, new Vector2(-0.70f, 0.06f), "DELETE CHARACTER", "opt_performance") { UIScale = buttonScale });
			manager.Elements.Add(new UIMenuButton(manager, new Vector2(-0.70f, -0.12f), "SELECT ANOTHER SERVER", "csel_servlist") { UIScale = buttonScale });
			manager.Elements.Add(new UIMenuButton(manager, new Vector2(-0.70f, -0.30f), "MAIN MENU", "csel_mainmenu") { UIScale = buttonScale });
			manager.Elements.Add(new UICharacterList(manager));
			manager.PlaySound("ui_motion_swish");
			manager.FlyInAll(FLYIN_LENGTH, 0.05);
		}

		void CharSelect_Disconnected(string obj)
		{
			netClient.Disconnected -= CharSelect_Disconnected;
			manager.FlyOutAll(FLYIN_LENGTH, 0.05);
			manager.PlaySound("ui_motion_swish");
			manager.AnimationComplete += ConstructServerList;
			netClient.Disconnected += ServerList_Disconnected;
		}

		void ConstructServerList()
		{
			manager.Elements.Clear();
			manager.AnimationComplete -= ConstructServerList;
			serverList = new UIServerList(manager) { Internet = internetServers };
			serverList.Selected += ServerList_Selected;
			manager.Elements.Add(serverList);
			manager.Elements.Add(new UIMenuButton(manager, new Vector2(0.01f, -0.55f), "SET FILTER", "srvlst_filter"));
			manager.Elements.Add(new UIMenuButton(manager, new Vector2(-0.64f, -0.55f), "MAIN MENU", "srvlst_mainmenu"));
			manager.FlyInAll(FLYIN_LENGTH, 0.05);
			//Refresh button - from right
			var rfrsh = new UIMenuButton(manager, new Vector2(0.67f, -0.55f), "REFRESH LIST", "srvlst_refresh");
			rfrsh.Animation = new FlyInRight(rfrsh.UIPosition, 0, FLYIN_LENGTH);
			rfrsh.Animation.Begin();
			manager.Elements.Add(rfrsh);
			//Connect button - from right
			connectButton = new UIMenuButton(manager, new Vector2(0.67f, -0.82f), "CONNECT >");
			connectButton.Animation = new FlyInRight(connectButton.UIPosition, 0, FLYIN_LENGTH);
			connectButton.Animation.Begin();
			manager.Elements.Add(connectButton);
			//SERVER DESCRIPTION - from right
			serverDescription = new UIServerDescription(manager, -0.32f, -0.81f) { ServerList = serverList };
			serverDescription.Animation = new FlyInRight(serverDescription.UIPosition, 0, FLYIN_LENGTH);
			serverDescription.Animation.Begin();
			manager.Elements.Add(serverDescription);
			manager.PlaySound("ui_motion_swish");
			if (netClient == null)
			{
				netClient = new GameClient(Game);
				netClient.Disconnected += ServerList_Disconnected;
				netClient.ServerFound += NetClient_ServerFound;
				netClient.Start();
				netClient.UUID = Game.Config.UUID.Value;
				netClient.CharacterSelection += (info) =>
				{
					csel = info;
					manager.FlyOutAll(FLYIN_LENGTH, 0.05);
					manager.PlaySound("ui_motion_swish");
					manager.AnimationComplete += ConstructCharacterSelect;
					netClient.Disconnected -= ServerList_Disconnected;
				};
			}
			netClient.DiscoverLocalPeers();
			if (internetServers)
				netClient.DiscoverGlobalPeers();
		}

		void ServerList_Disconnected(string reason)
		{

		}
		void NetClient_ServerFound(LocalServerInfo obj)
		{
			serverList.Servers.Add(obj);
		}

		void ConstructOptions()
		{
			manager.Elements.Clear();
			manager.AnimationComplete -= ConstructOptions;
			manager.Elements.Add(new UIMenuButton(manager, new Vector2(-0.65f, 0.5f), "GENERAL", "opt_general"));
			manager.Elements.Add(new UIMenuButton(manager, new Vector2(-0.65f, 0.25f), "CONTROLS", "opt_controls"));
			manager.Elements.Add(new UIMenuButton(manager, new Vector2(-0.65f, 0.0f), "PERFORMANCE", "opt_performance"));
			manager.Elements.Add(new UIMenuButton(manager, new Vector2(-0.65f, -0.25f), "AUDIO", "opt_audio"));
			manager.Elements.Add(new UIMenuButton(manager, new Vector2(-0.65f, -0.50f), "CREDITS", "opt_credits"));
			manager.Elements.Add(new UIMenuButton(manager, new Vector2(-0.65f, -0.75f), "MAIN MENU", "opt_mainmenu"));
			manager.PlaySound("ui_motion_swish");
			manager.FlyInAll(FLYIN_LENGTH, 0.05);
		}

		void ConstructMainMenu()
		{
			manager.Elements.Clear();
			manager.AnimationComplete -= ConstructMainMenu;
			manager.Elements.Add(new UIMenuButton(manager, new Vector2(-0.65f, 0.40f), "GAMEPLAY DEMO", "gameplay"));
			manager.Elements.Add(new UIMenuButton(manager, new Vector2(-0.65f, 0.15f), "SYSTEM VIEWER", "system"));
			manager.Elements.Add(new UIMenuButton(manager, new Vector2(-0.65f, -0.1f), "MULTIPLAYER", "multiplayer"));
			manager.Elements.Add(new UIMenuButton(manager, new Vector2(-0.65f, -0.35f), "OPTIONS", "options"));
			manager.Elements.Add(new UIMenuButton(manager, new Vector2(-0.65f, -0.6f), "EXIT", "exit"));
			manager.PlaySound("ui_motion_swish");
			manager.FlyInAll(FLYIN_LENGTH, 0.05);
		}

		public override void Draw (TimeSpan delta)
		{
			//Make sure delta time is normal
			if (dframes == 0)
			{
				dframes = 1;
				return;
			}
			//TODO: Draw background THN
			scene.Draw();
			//UI Background
			Game.Renderer2D.Start (Game.Width, Game.Height);
			Game.Renderer2D.DrawImageStretched (logoOverlay, new Rectangle (0, 0, Game.Width, Game.Height), Color4.White, true);
			Game.Renderer2D.Finish ();
			//buttons
			manager.Draw();
			//Cursor
			Game.Renderer2D.Start(Game.Width, Game.Height);
			cur.Draw(Game.Renderer2D, Game.Mouse);
			Game.Renderer2D.Finish();
		}
	}
}

