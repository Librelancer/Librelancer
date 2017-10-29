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
	public class HudGaugeElement : HudModelElement
	{
		float gaugeHeight = 0.023f;

		Vector2 powerPosition = new Vector2(-0.1465f, -0.9135f);
		Vector2 shieldPosition = new Vector2(-0.1465f, -0.94f);
		Vector2 hullPosition = new Vector2(-0.1465f, -0.966f);

		Color4 PowerColor = new Color4(0xA7, 0xA1, 0x5E, 0xFF);
		public Color4 ShieldColor = new Color4(0x3E, 0x3D, 0xB5, 0xFF);
		public Color4 HullColor = new Color4(0x78, 0x2A, 0x33, 0xFF);

		Texture2D gauge_mask;

		public float PowerPercentage = 1f;
		public float ShieldPercentage = 1f;
		public float HullPercentage = 1f;

		public HudGaugeElement(UIManager manager) : base(manager, "hud_guagewindow.cmp" /*typo intentional*/, 0.01f, -0.95f, 1.95f, 2.75f)
		{
			gauge_mask = ImageLib.Generic.FromStream(typeof(Hud).Assembly.GetManifestResourceStream("LibreLancer.Shaders.gauge_mask.png"));
			gauge_mask.SetFiltering(TextureFiltering.Nearest);
		}

		public override void DrawText()
		{
			DrawBar(powerPosition, PowerColor, PowerPercentage);
			DrawBar(shieldPosition, ShieldColor, ShieldPercentage);
			DrawBar(hullPosition, HullColor, HullPercentage);
		}

		void DrawBar(Vector2 position, Color4 color, float pct)
		{
			var p1 = IdentityCamera.Instance.ScreenToPixel(position.X, position.Y);
			var p2 = IdentityCamera.Instance.ScreenToPixel(position.X, position.Y - gaugeHeight);
			int ph = (int)(p2.Y - p1.Y);
			float scaleFactor = ph / (float)gauge_mask.Height;
			int pw = (int)((gauge_mask.Width * scaleFactor) * pct);
			var src = new Rectangle(0, 0, (int)(gauge_mask.Width * pct), gauge_mask.Height);
			Manager.Game.Renderer2D.FillRectangleMask(gauge_mask, src, new Rectangle((int)p1.X, (int)p1.Y, pw, ph), color);
		}
	}
}
