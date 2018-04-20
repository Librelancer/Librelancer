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
    public partial class MainMenu : GameState
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
            g.CursorKind = CursorKind.None;

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
            scene.Dispose();
        }


        void G_WillClose()
        {
            if (netClient != null) netClient.Dispose();
        }

        int frames = 0;
        int dframes = 0;
        public override void Update(TimeSpan delta)
        {
            //Don't want the big lag at the start
            if (frames == 0)
            {
                frames = 1;
                return;
            }
            scene.Update(delta);
            manager.Update(delta);
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

        void ConstructOptions()
        {
            manager.Elements.Clear();
            manager.AnimationComplete -= ConstructOptions;
            manager.Elements.Add(new UIMenuButton(manager, new Vector2(-0.65f, 0.5f), "GENERAL", null));
            manager.Elements.Add(new UIMenuButton(manager, new Vector2(-0.65f, 0.25f), "CONTROLS", null));
            manager.Elements.Add(new UIMenuButton(manager, new Vector2(-0.65f, 0.0f), "PERFORMANCE", null));
            manager.Elements.Add(new UIMenuButton(manager, new Vector2(-0.65f, -0.25f), "AUDIO", null));
            manager.Elements.Add(new UIMenuButton(manager, new Vector2(-0.65f, -0.50f), "CREDITS", null));
            manager.Elements.Add(new UIMenuButton(manager, new Vector2(-0.65f, -0.75f), "MAIN MENU", OptionsToMainMenu));
            manager.PlaySound("ui_motion_swish");
            manager.FlyInAll(FLYIN_LENGTH, 0.05);
        }

        void OptionsToMainMenu()
        {
            manager.PlaySound("ui_motion_swish");
            manager.FlyOutAll(FLYIN_LENGTH, 0.05);
            manager.AnimationComplete += ConstructOptions;
        }

        void ConstructMainMenu()
        {
            manager.Elements.Clear();
            manager.AnimationComplete -= ConstructMainMenu;
            manager.Elements.Add(new UIMenuButton(manager, new Vector2(-0.65f, 0.40f), "GAMEPLAY DEMO", DoGameplay));
            manager.Elements.Add(new UIMenuButton(manager, new Vector2(-0.65f, 0.15f), "SYSTEM VIEWER",DoSystemView));
            manager.Elements.Add(new UIMenuButton(manager, new Vector2(-0.65f, -0.1f), "MULTIPLAYER", ConstructLanInternetDialog));
            manager.Elements.Add(new UIMenuButton(manager, new Vector2(-0.65f, -0.35f), "OPTIONS", DoOptions));
            manager.Elements.Add(new UIMenuButton(manager, new Vector2(-0.65f, -0.6f), "EXIT", Exit));
            manager.PlaySound("ui_motion_swish");
            manager.FlyInAll(FLYIN_LENGTH, 0.05);
        }

        void DoGameplay()
        {
            manager.PlaySound("ui_motion_swish");
            manager.FlyOutAll(FLYIN_LENGTH, 0.05);
            manager.AnimationComplete += () =>
            {
                manager.Dispose();
                Game.ChangeState(new SpaceGameplay(Game, new GameSession(Game)));
            };
        }
        void DoSystemView()
        {
            manager.PlaySound("ui_motion_swish");
            manager.FlyOutAll(FLYIN_LENGTH, 0.05);
            manager.AnimationComplete += () =>
            {
                manager.Dispose();
                Game.ChangeState(new DemoSystemView(Game));
            };
        }
        void DoOptions()
        {
            
        }
        void Exit()
        {
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

