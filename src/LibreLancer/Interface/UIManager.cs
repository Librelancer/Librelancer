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
namespace LibreLancer
{
	public class UIManager
	{
		public IDrawable MenuButton;
		public FreelancerGame Game;
		public Color4 TextColor = new Color4 (160, 196, 210, 255);
		public List<UIElement> Elements = new List<UIElement> ();
		public event Action<string> Clicked;
		Font buttonFont;
		float currentSize = -2f;
		public UIManager (FreelancerGame game)
		{
			Game = game;
		}
		public Font GetButtonFont(float sz)
		{
			if (currentSize != sz) {
				if (buttonFont != null)
					buttonFont.Dispose ();
				currentSize = sz;
				buttonFont = Font.FromSystemFont (Game.Renderer2D, "Agency FB", currentSize);
			}
			return buttonFont;
		}
		public void Draw()
		{
			Game.RenderState.DepthEnabled = false;
			foreach (var e in Elements)
				e.DrawBase ();
			Game.Renderer2D.Start (Game.Width, Game.Height);
			foreach (var e in Elements)
				e.DrawText ();
			Game.Renderer2D.Finish ();
		}
		public void FlyInAll(double duration, double spacing)
		{
			double currentspacing = 0;
			foreach (var elem in Elements)
			{
				elem.Animation = new FlyInLeft(
					elem.UIPosition,
					currentspacing,
					duration
				);
				elem.Animation.Begin();
				currentspacing += spacing;
			}
		}
		public void OnClick(string tag)
		{
			if (Clicked != null)
				Clicked (tag);
		}

		public void Update(TimeSpan delta)
		{
			MenuButton.Update (IdentityCamera.Instance, delta);
			foreach (var elem in Elements)
				elem.Update (delta);
		}

		public Vector2 ScreenToPixel (float screenx, float screeny)
		{
			float distx = screenx * (Game.Width / 2);
			float x = (Game.Width / 2) + distx;

			float disty = screeny * (Game.Height / 2);
			float y = (Game.Height / 2) - disty;

			return new Vector2 (x, y);
		}
	}
}

