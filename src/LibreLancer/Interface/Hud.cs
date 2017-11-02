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

namespace LibreLancer
{
	public class Hud
	{
		UIManager manager;

		HudModelElement shipinfo;
		HudModelElement contactslist;
		HudNavBox navbuttons;
		HudChatBox chatbox;
		HudNumberBoxElement numberbox;
		HudGaugeElement gauge;

		public float PowerPercentage = 1f;
		public float ShieldPercentage = 1f;
		public float HullPercentage = 1f;
		public float Velocity;
		public float ThrustAvailable = 1f;
		public bool ShowMouseFlight = false;
		public bool CruiseCharging = false;
		public GameObject SelectedObject = null;

		IDrawable reticle;
		IDrawable reticle_arrows;
		IDrawable reticle_health;
		IDrawable reticle_quotes;
		IDrawable reticle_shields;

		RenderMaterial UI_HUD_targetarrow;
		RenderMaterial UI_HUD_targetingblue;

		List<Maneuver> mnvs;
		public Hud(FreelancerGame game, List<GameData.BaseHotspot> hotspots = null)
		{
			manager = new UIManager(game);
			//backgrounds
			contactslist = new HudModelElement(manager, "hud_target.cmp", -0.73f, -0.69f, 2.1f, 2.9f);
			manager.Elements.Add(contactslist);

			shipinfo = new HudModelElement(manager, "hud_shipinfo.cmp", 0.73f, -0.69f, 2.1f, 2.9f);
			manager.Elements.Add(shipinfo);

			numberbox = new HudNumberBoxElement(manager);
			manager.Elements.Add(numberbox);

			gauge = new HudGaugeElement(manager);
			manager.Elements.Add(gauge);

			chatbox = new HudChatBox(manager);
			manager.Elements.Add(chatbox);

			//Maneuvers
			if (hotspots == null)
			{
				mnvs = game.GameData.GetManeuvers().ToList();
				navbuttons = new HudNavBox(mnvs, manager);
			}
			else
			{
				navbuttons = new HudNavBox(game.GameData.GetBaseNavbarIcons(), hotspots, manager);
			}
			navbuttons.Show();

			manager.Clicked += Manager_OnClick;

			reticle = game.ResourceManager.GetDrawable(game.GameData.ResolveDataPath("INTERFACE/HUD/hud_reticle.3db"));
			reticle_health = game.ResourceManager.GetDrawable(game.GameData.ResolveDataPath("INTERFACE/HUD/hud_reticle_health.3db"));
			reticle_shields = game.ResourceManager.GetDrawable(game.GameData.ResolveDataPath("INTERFACE/HUD/hud_reticle_shields.3db"));

			UI_HUD_targetarrow = game.ResourceManager.FindMaterial(CrcTool.FLModelCrc("UI_HUD_targetarrow")).Render;
			UI_HUD_targetingblue = game.ResourceManager.FindMaterial(CrcTool.FLModelCrc("UI_HUD_targetingblue")).Render;

			TextEntry = false;
		}

		bool roomMode = false;
		public void RoomMode()
		{
			roomMode = true;
			//Hide non-room controls
			gauge.Visible = false;
			contactslist.Visible = false;
			numberbox.Visible = false;
			shipinfo.Visible = false;
		}

		void Manager_OnClick(string obj)
		{
			if (obj.StartsWith("mnv"))
				navbuttons.ProcessClick(obj, OnManeuver);
		}

		public event Func<string, bool> OnManeuverSelected;
		bool OnManeuver(string action)
		{
			Console.WriteLine(action);
			if (OnManeuverSelected != null)
				return OnManeuverSelected(action);
			return false;
		}

		ICamera gameCamera;
		public void Update(TimeSpan delta, ICamera camera)
		{
			gameCamera = camera;
			numberbox.Velocity = Velocity;
			numberbox.ThrustAvailable = ThrustAvailable;
			manager.Update(delta);
		}

		//Get size of text used for mouse flight
		float GetStatusTextSize(float px)
		{
			return 12f;
		}

		public bool TextEntry
		{
			get { return chatbox.Visible; }
			set { chatbox.Visible = value; }
		}
		public void OnTextEntry(string e)
		{
			chatbox.AppendText(e);
		}

		public event Action<string> OnEntered;

		public void TextEntryKeyPress(Keys k)
		{
			if (k == Keys.Enter)
			{
				TextEntry = false;
				if (OnEntered != null)
					OnEntered(chatbox.CurrentText);
				chatbox.CurrentText = "";
			}
			if (k == Keys.Backspace)
			{
				if (chatbox.CurrentText.Length > 0)
					chatbox.CurrentText = chatbox.CurrentText.Substring(0, chatbox.CurrentText.Length - 1);
			}
			if (k == Keys.Escape)
			{
				TextEntry = false;
				chatbox.CurrentText = "";
			}
		}

		public void Draw()
		{
			if (SelectedObject != null)
			{
				manager.Game.RenderState.Cull = false;
				manager.Game.RenderState.DepthEnabled = false;
				if (SelectedObject.RenderComponent.OutOfView(gameCamera))
				{
					//Render one of them fancy arrows
				}
				else
				{
					//Render the hud
					var vp = gameCamera.ViewProjection;
					var translation = SelectedObject.GetTransform();
					//project centre point
					var projected = new Vector4(0, 0, 0, 1) * (translation * vp);
					projected /= projected.W;
					projected.Z = 1f;
					//do translation
					var translateSelection = Matrix4.CreateTranslation(projected.X, projected.Y, projected.Z);
					//HACK: I don't think modifying the materials exactly is what I'm supposed to do.
					var gaugeMat = (BasicMaterial)UI_HUD_targetarrow;
					var boxMat = (BasicMaterial)UI_HUD_targetingblue;
					gaugeMat.Dc = Color4.White;
					boxMat.Dc = Color4.Green;
					reticle.Update(IdentityCamera.Instance, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(0));
					reticle.Draw(manager.Game.RenderState, translateSelection, Lighting.Empty);
					//Hull Gauge
					gaugeMat.Dc = gauge.HullColor;
					reticle_health.Update(IdentityCamera.Instance, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(0));
					reticle_health.Draw(manager.Game.RenderState, translateSelection, Lighting.Empty);
					//Shield Gauge
					gaugeMat.Dc = gauge.ShieldColor;
					reticle_shields.Update(IdentityCamera.Instance, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(0));
					reticle_shields.Draw(manager.Game.RenderState, translateSelection, Lighting.Empty);
				}
				manager.Game.RenderState.DepthEnabled = true;
				manager.Game.RenderState.Cull = true;
			}
			manager.Draw();

		}
	}
}