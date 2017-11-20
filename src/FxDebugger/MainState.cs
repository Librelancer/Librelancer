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

//TODO: Redesign UI2D to do containers properly so this is less ugly
using System;
using System.Collections.Generic;
namespace LibreLancer.FxDebugger
{
	public class MainState : GameState
	{
		UIManager manager;
		RectangleElement2D menuBackground;
		Font uiFont;

		SystemRenderer renderer;
		ParticleEffectRenderer pfx;

		ChaseCamera cam;
		SliderElement2D zoomSlider;
		public MainState(FreelancerGame game) : base(game)
		{
			manager = new UIManager(game);
			uiFont = Font.FromSystemFont(game.Renderer2D, "Arial", 10);
			//Construct Menu
			menuBackground = new RectangleElement2D(manager);
			menuBackground.FillColor = new Color4(0, 0, 0, 0.25f);
			manager.Elements.Add(menuBackground);
			var btnOpen = new ButtonElement2D(manager, uiFont) { Label = "Open" };
			btnOpen.AutoSize(Game.Renderer2D);
			btnOpen.Position2D = new Vector2(10, 10);
			btnOpen.Clicked += OpenFxDialog;
			menuBackground.Height = 20 + btnOpen.Height;
			manager.Elements.Add(btnOpen);

			var btnRefresh = new ButtonElement2D(manager, uiFont) { Label = "Refresh" };
			btnRefresh.AutoSize(Game.Renderer2D);
			btnRefresh.Position2D = new Vector2(10 + btnOpen.Position2D.X + btnOpen.Width, 10);
			btnRefresh.Clicked += () => {
				if (currentOpen != null) DoOpenFx(currentOpen);
			};
			manager.Elements.Add(btnRefresh);

			var btnOptions = new ButtonElement2D(manager, uiFont) { Label = "Options" };
			btnOptions.AutoSize(Game.Renderer2D);
			btnOptions.Position2D = new Vector2(10 + btnRefresh.Position2D.X + btnRefresh.Width, 10);
			btnOptions.Clicked += OptionsDialog;
			manager.Elements.Add(btnOptions);

			var btnExit = new ButtonElement2D(manager, uiFont) { Label = "Exit" };
			btnExit.AutoSize(Game.Renderer2D);
			btnExit.Position2D = new Vector2(10 + btnOptions.Position2D.X + btnOptions.Width, 10);
			btnExit.Clicked += () => MessageDialog("Are you sure you want to exit?", Game.Exit);
			manager.Elements.Add(btnExit);

			zoomSlider = new SliderElement2D(manager, uiFont) { Label = "Zoom:" };
			zoomSlider.AutoSize(Game.Renderer2D);
			zoomSlider.Position2D = new Vector2(10 + btnExit.Position2D.X + btnExit.Width, 10);
			manager.Elements.Add(zoomSlider);

			//Setup input and rendering
			Game.Keyboard.TextInput += Keyboard_TextInput;
			Game.Keyboard.KeyDown += Keyboard_KeyDown;
			cam = new ChaseCamera(Game.Viewport);
			renderer = new SystemRenderer(cam, Game.GameData, Game.ResourceManager);
			renderer.NullColor = new Color4(0.1072961f, 0.1587983f, 0.1845494f, 1);
		}


		void OptionsDialog()
		{
			var dlg = new List<UIElement>();
			dlg.Add(new RectangleElement2D(manager) { FillColor = new Color4(0, 0, 0, 0.4f), Fullscreen = true });
			var bkg = new RectangleElement2D(manager) { FillColor = Color4.White, Width = 300, Height = 300 };
			bkg.CalculatePosition += () =>
			{
				bkg.Position2D = new Vector2(Game.Width / 2 - 150, Game.Height / 2 - 150);
			};
			dlg.Add(bkg);

			var lbl = new LabelElement2D(manager, uiFont) { Text = "Background Color:" };
			lbl.CalculatePosition += () =>
			{
				lbl.Position2D = new Vector2(Game.Width / 2 - 150 + 10, Game.Height / 2 - 150 + 10);
			};
			dlg.Add(lbl);

			var zoomR = new SliderElement2D(manager, uiFont) { Label = "R:", BlackText = true, Minimum = 0, Value = renderer.NullColor.R, Maximum = 1 };
			zoomR.AutoSize(Game.Renderer2D);
			zoomR.CalculatePosition += () =>
			{
				zoomR.Position2D = new Vector2(Game.Width / 2 - 150 + 10, Game.Height / 2 - 150 + 50);

			};
			dlg.Add(zoomR);

			var zoomG = new SliderElement2D(manager, uiFont) { Label = "G:", BlackText = true, Minimum = 0, Value = renderer.NullColor.G, Maximum = 1 };
			zoomG.AutoSize(Game.Renderer2D);
			zoomG.CalculatePosition += () =>
			{
				zoomG.Position2D = new Vector2(Game.Width / 2 - 150 + 10, Game.Height / 2 - 150 + 100);

			};
			dlg.Add(zoomG);

			var zoomB = new SliderElement2D(manager, uiFont) { Label = "B:", BlackText = true, Minimum = 0, Value = renderer.NullColor.B, Maximum = 1 };
			zoomB.AutoSize(Game.Renderer2D);
			zoomB.CalculatePosition += () =>
			{
				zoomB.Position2D = new Vector2(Game.Width / 2 - 150 + 10, Game.Height / 2 - 150 + 150);
			};
			dlg.Add(zoomB);

			var prev = new RectangleElement2D(manager) { FillColor = renderer.NullColor };
			prev.Width = prev.Height = zoomB.Height;
			prev.CalculatePosition += () =>
			{
				prev.Position2D = new Vector2(Game.Width / 2 - 150 + 250, Game.Height / 2 - 150 + 10);
				prev.FillColor = new Color4(zoomR.Value, zoomG.Value, zoomB.Value, 1f);
			};

			var btnCancel = new ButtonElement2D(manager, uiFont) { Label = "Cancel" };
			btnCancel.AutoSize(Game.Renderer2D);

			var btnOk = new ButtonElement2D(manager, uiFont) { Label = "OK" };
			btnOk.Width = btnCancel.Width;
			btnOk.Height = btnCancel.Height;
			btnOk.Clicked += () =>
			{
				renderer.NullColor = new Color4(zoomR.Value, zoomG.Value, zoomB.Value, 1f);
				manager.Dialog = null;
			};

			btnCancel.Clicked += () => manager.Dialog = null;
			btnOk.CalculatePosition += () =>
			{
				btnOk.Position2D = new Vector2(Game.Width / 2 - (btnOk.Width + btnCancel.Width + 5) / 2, Game.Height / 2 + 50);
			};
			btnCancel.CalculatePosition += () =>
			{
				btnCancel.Position2D = new Vector2(Game.Width / 2 + 5, Game.Height / 2 + 50);
			};
			dlg.Add(btnOk);
			dlg.Add(btnCancel);

			dlg.Add(prev);
			manager.Dialog = dlg;
		}

		HudChatBox entry;
		string currentOpen = null;
		void Keyboard_TextInput(string text)
		{
			if (entry == null) return;
			entry.AppendText(text);
		}

		void Keyboard_KeyDown(KeyEventArgs e)
		{
			if (entry == null)
			{
				if ((e.Modifiers & KeyModifiers.LeftAlt) == KeyModifiers.LeftAlt && e.Key == Keys.Enter) {
					Game.ToggleFullScreen();
				}
				Console.WriteLine("{0} {1}", e.Modifiers, e.Key);
				return;
			}
			if (e.Key == Keys.Enter)
			{
				Game.DisableTextInput();
				DoOpenFx(entry.CurrentText.Trim());
				entry = null;
			}
			if (e.Key == Keys.Backspace)
			{
				if (entry.CurrentText.Length > 0)
					entry.CurrentText = entry.CurrentText.Substring(0, entry.CurrentText.Length - 1);
			}
			if (e.Key == Keys.Escape) //Cancel
			{
				Game.DisableTextInput();
				entry = null;
				manager.Dialog = null;
			}
		}

		void OpenFxDialog()
		{
			var dlg = new List<UIElement>();
			dlg.Add(new RectangleElement2D(manager) { FillColor = new Color4(0, 0, 0, 0.4f), Fullscreen = true });
			entry = new HudChatBox(manager) { CentreScreen = true, CurrentEntry = "Effect->" };
			dlg.Add(entry);
			Game.EnableTextInput();
			manager.Dialog = dlg;
		}

		void DoOpenFx(string name)
		{
			manager.Dialog = null;
			if (!Game.GameData.HasEffect(name))
			{
				MessageDialog(string.Format("The effect {0} was not found. Check that it is defined in an effect ini?", name));
				return;
			}
			currentOpen = name;
			Game.ResourceManager.ClearTextures(); //Don't wanna use all our memory up!
			var fx = Game.GameData.GetEffect(name);
			if (pfx != null) pfx.Unregister();
			pfx = new ParticleEffectRenderer(fx);
			pfx.Register(renderer);
		}

		void MessageDialog(string text, Action yes = null)
		{
			var dlg = new List<UIElement>();
			dlg.Add(new RectangleElement2D(manager) { FillColor = new Color4(0, 0, 0, 0.4f), Fullscreen = true });
			var bkg = new RectangleElement2D(manager) { FillColor = Color4.White, Width = 300, Height = 200 };
			bkg.CalculatePosition += () =>
			{
				bkg.Position2D = new Vector2(Game.Width / 2 - 150, Game.Height / 2 - 100);
			};
			dlg.Add(bkg);
			var lbl = new LabelElement2D(manager, uiFont);
			int a, b = 0;
			lbl.Text = string.Join("\n", Infocards.InfocardDisplay.WrapText(Game.Renderer2D, uiFont, text, 280, 0, out a, ref b));
			lbl.CalculatePosition += () =>
			{
				lbl.Position2D = new Vector2(Game.Width / 2 - 150 + 10, Game.Height / 2 - 100 + 10);
			};
			dlg.Add(lbl);
			if (yes == null)
			{
				var btnOk = new ButtonElement2D(manager, uiFont) { Label = "OK" };
				btnOk.AutoSize(Game.Renderer2D);
				btnOk.CalculatePosition += () =>
				{
					btnOk.Position2D = new Vector2(Game.Width / 2 - (btnOk.Width / 2), Game.Height / 2 + 50);
				};
				btnOk.Clicked += () => manager.Dialog = null;
				dlg.Add(btnOk);
			}
			else
			{
				var btnYes = new ButtonElement2D(manager, uiFont) { Label = "Yes" };
				btnYes.AutoSize(Game.Renderer2D);
				btnYes.Clicked += yes;
				var btnNo = new ButtonElement2D(manager, uiFont) { Label = "No" };
				btnNo.Width = btnYes.Width;
				btnNo.Height = btnYes.Height;
				btnNo.Clicked += () => manager.Dialog = null;
				btnYes.CalculatePosition += () =>
				{
					btnYes.Position2D = new Vector2(Game.Width / 2 - (btnYes.Width + btnNo.Width + 5) / 2, Game.Height / 2 + 50);
				};
				btnNo.CalculatePosition += () =>
				{
					btnNo.Position2D = new Vector2(Game.Width / 2 + 5, Game.Height / 2 + 50);
				};
				dlg.Add(btnYes);
				dlg.Add(btnNo);
			}
			manager.Dialog = dlg;
		}

		public override void Draw(TimeSpan delta)
		{
			renderer.Draw();
			manager.Draw();
		}

		public override void Update(TimeSpan delta)
		{
			//Fixed camera (i know)
			cam.Viewport = Game.Viewport;
			cam.ChasePosition = Vector3.Zero;
			cam.ChaseOrientation = Matrix4.CreateRotationX(MathHelper.Pi);
			cam.DesiredPositionOffset = new Vector3(zoomSlider.Minimum + (zoomSlider.Maximum - zoomSlider.Value), 0, 0);
			cam.OffsetDirection = Vector3.UnitX;
			cam.Reset();
			cam.Update(TimeSpan.FromSeconds(500));
			//Update
			if (pfx != null) pfx.Update(delta, Vector3.Zero, Matrix4.Identity);
			//Other
			renderer.Update(delta);
			menuBackground.Width = Game.Width;
			manager.Update(delta);
		}

		public override void Unregister()
		{
			Game.Keyboard.TextInput -= Keyboard_TextInput;
			Game.Keyboard.KeyDown -= Keyboard_KeyDown;
			manager.Dispose();
		}
	}
}
