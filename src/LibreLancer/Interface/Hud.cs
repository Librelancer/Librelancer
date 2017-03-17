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
namespace LibreLancer
{
	public class Hud
	{
		//Windows
		IDrawable hud_maneuverbox1;
		IDrawable hud_maneuverbox2;
		IDrawable hud_maneuverbox3;
		IDrawable hud_maneuverbox4;
		IDrawable hud_maneuverbox5;
		IDrawable hud_maneuverbox6;

		IDrawable hud_target;
		IDrawable hud_shipinfo;
		IDrawable hud_gaugewindow;
		IDrawable hud_numberboxes;

		//Elements
		Texture2D gauge_mask; //mask texture for creating power/hull/shield gauges


		IDrawable L(FreelancerGame game, string path)
		{
			return game.ResourceManager.GetDrawable(game.GameData.ResolveDataPath("INTERFACE/HUD/" + path));
		}

		//Transforms
		Matrix4 hud_gaugetransform;
		Matrix4 hud_targettransform;
		Matrix4 hud_shipinfotransform;
		Matrix4 hud_maneuverstransform;
		Matrix4 hud_numberboxestransform;

		public Hud(FreelancerGame game)
		{
			hud_maneuverbox1 = L(game, "hud_maneuverbox1.cmp");
			hud_maneuverbox2 = L(game, "hud_maneuverbox2.cmp");
			hud_maneuverbox3 = L(game, "hud_maneuverbox3.cmp");
			hud_maneuverbox4 = L(game, "hud_maneuverbox4.cmp");
			hud_maneuverbox5 = L(game, "hud_maneuverbox5.cmp");
			hud_maneuverbox6 = L(game, "hud_maneuverbox6.cmp");

			hud_target = L(game, "hud_target.cmp");
			hud_shipinfo = L(game, "hud_shipinfo.cmp");
			hud_gaugewindow = L(game, "hud_guagewindow.cmp"); //This is how it's spelt in Freelancer
			gauge_mask = ImageLib.Generic.FromStream(typeof(Hud).Assembly.GetManifestResourceStream("LibreLancer.Shaders.gauge_mask.png"));
			gauge_mask.SetFiltering(TextureFiltering.Nearest);

			hud_numberboxes = L(game, "hud_numberboxes.cmp");
			//Set Transforms
			hud_gaugetransform = Matrix4.CreateScale(1.95f, 2.75f, 1) * Matrix4.CreateTranslation(0.01f, -0.95f, 0);
			hud_targettransform = Matrix4.CreateScale(2.1f, 2.9f, 1) * Matrix4.CreateTranslation(-0.73f, -0.69f, 0);
			hud_shipinfotransform = Matrix4.CreateScale(2.1f, 2.9f, 1) * Matrix4.CreateTranslation(0.73f, -0.69f, 0);
			hud_maneuverstransform = Matrix4.CreateScale(4.5f, 6f, 0) * Matrix4.CreateTranslation(0, 0.925f, 1);
			hud_numberboxestransform = Matrix4.CreateScale(1.93f, 2.5f, 0) * Matrix4.CreateTranslation(0.01f, -0.952f, 0);
		}

		public void Draw(FreelancerGame game)
		{
			game.RenderState.DepthEnabled = false;
			DrawStatusGauge(game);
			DrawNumberBoxes(game);
			DrawNavButtons(game);
			DrawContactsList(game);
			DrawShipInfo(game);
		}

		float gaugeHeight = 0.023f;

		Vector2 powerPosition = new Vector2(-0.1465f, -0.9135f);
		Vector2 shieldPosition = new Vector2(-0.1465f, -0.94f);
		Vector2 hullPosition = new Vector2(-0.1465f, -0.966f);

		Color4 powerColor = new Color4(0xA7, 0xA1, 0x5E, 0xFF);
		Color4 shieldColor = new Color4(0x3E, 0x3D, 0xB5, 0xFF);
		Color4 hullColor = new Color4(0x78, 0x2A, 0x33, 0xFF);

		public float PowerPercentage = 1f;
		public float ShieldPercentage = 1f;
		public float HullPercentage = 1f;

		void DrawStatusGauge(FreelancerGame game)
		{
			hud_gaugewindow.Update(IdentityCamera.Instance, TimeSpan.Zero, TimeSpan.FromSeconds(game.TotalTime));
			hud_gaugewindow.Draw(game.RenderState, hud_gaugetransform, Lighting.Empty);

			game.Renderer2D.Start(game.Width, game.Height);
			DrawBar(game, powerPosition, powerColor, PowerPercentage);
			DrawBar(game, shieldPosition, shieldColor, ShieldPercentage);
			DrawBar(game, hullPosition, hullColor, HullPercentage);
			game.Renderer2D.Finish();
		}

		void DrawBar(FreelancerGame g, Vector2 position, Color4 color, float pct)
		{
			var p1 = IdentityCamera.Instance.ScreenToPixel(position.X, position.Y);
			var p2 = IdentityCamera.Instance.ScreenToPixel(position.X, position.Y - gaugeHeight);
			int ph = (int)(p2.Y - p1.Y);
			float scaleFactor = ph / (float)gauge_mask.Height;
			int pw = (int)((gauge_mask.Width * scaleFactor) * pct);
			var src = new Rectangle(0, 0, (int)(gauge_mask.Width * pct), gauge_mask.Height);
			g.Renderer2D.FillRectangleMask(gauge_mask, src, new Rectangle((int)p1.X, (int)p1.Y, pw, ph), color);
		}

		Rectangle FromScreenRect(float screenx, float screeny, float screenw, float screenh)
		{
			var p1 = IdentityCamera.Instance.ScreenToPixel(screenx, screeny);
			var p2 = IdentityCamera.Instance.ScreenToPixel(screenx + screenw, screeny - screenh);
			return new Rectangle(
				(int)(p1.X),
				(int)(p1.Y),
				(int)(p2.X - p1.X),
				(int)(p2.Y - p1.Y)
			);
		}

		Font numberFont;
		float numberSize = -1;
		Font GetNumbersFont(float sz, FreelancerGame g)
		{
			if (numberSize != sz)
			{
				if (numberFont != null)
					numberFont.Dispose();
				numberSize = sz;
				numberFont = Font.FromSystemFont(g.Renderer2D, "Agency FB", numberSize);
			}
			return numberFont;
		}

		float GetTextSize(float px)
		{
			return (px * (72.0f / 96.0f));
		}

		protected void DrawShadowedText(Renderer2D r, Font font, string text, float x, float y, Color4 c)
		{
			r.DrawString(font, text, x + 2, y + 2, Color4.Black);
			r.DrawString(font, text, x, y, c);
		}

		protected void DrawTextCentered(Renderer2D r, Font font, string text, Rectangle rect, Color4 c)
		{
			var size = r.MeasureString(font, text);
			var pos = new Vector2(
				rect.X + (rect.Width / 2f - size.X / 2),
				rect.Y + (rect.Height / 2f - size.Y / 2)
			);
			DrawShadowedText(r, font, text, pos.X, pos.Y, c);
		}

		public Color4 TextColor = new Color4(160, 196, 210, 255);
		public float ThrustAvailable = 1;
		public float Velocity = 0;
		void DrawNumberBoxes(FreelancerGame game)
		{
			hud_numberboxes.Update(IdentityCamera.Instance, TimeSpan.Zero, TimeSpan.FromSeconds(game.TotalTime));
			hud_numberboxes.Draw(game.RenderState, hud_numberboxestransform, Lighting.Empty);

			var thrustbox = FromScreenRect(-0.2925f, -0.93f, 0.077f, 0.055f);
			var speedbox = FromScreenRect(0.231f, -0.93f, 0.077f, 0.055f);
			var font = GetNumbersFont(GetTextSize(thrustbox.Height), game);
			game.Renderer2D.Start(game.Width, game.Height);
			DrawTextCentered(game.Renderer2D, font, (int)MathHelper.Clamp(ThrustAvailable * 100, 0, 100) + "%", thrustbox, TextColor);
			DrawTextCentered(game.Renderer2D, font, ((int)Velocity).ToString(), speedbox, TextColor);
			game.Renderer2D.Finish();
		}

		void DrawNavButtons(FreelancerGame g)
		{
			//TODO : Extend Nav Buttons to support a variable amount of maneuvers
			var mbox = hud_maneuverbox4;
			mbox.Update(IdentityCamera.Instance, TimeSpan.Zero, TimeSpan.FromSeconds(g.TotalTime));
			mbox.Draw(g.RenderState, hud_maneuverstransform, Lighting.Empty);
		}

		void DrawContactsList(FreelancerGame g)
		{
			hud_target.Update(IdentityCamera.Instance, TimeSpan.Zero, TimeSpan.FromSeconds(g.TotalTime));
			hud_target.Draw(g.RenderState, hud_targettransform, Lighting.Empty);
		}

		void DrawShipInfo(FreelancerGame g)
		{
			hud_shipinfo.Update(IdentityCamera.Instance, TimeSpan.Zero, TimeSpan.FromSeconds(g.TotalTime));
			hud_shipinfo.Draw(g.RenderState, hud_shipinfotransform, Lighting.Empty);
		}

	}
}