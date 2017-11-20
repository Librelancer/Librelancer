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
	public class LoadingDataState : GameState
	{
		Texture2D splash;
		public LoadingDataState(FreelancerGame g) : base(g)
		{
			splash = g.GameData.GetSplashScreen();
		}
		public override void Draw(TimeSpan delta)
		{
			Game.Renderer2D.Start(Game.Width, Game.Height);
			Game.Renderer2D.DrawImageStretched(splash, new Rectangle(0, 0, Game.Width, Game.Height), Color4.White, true);
			Game.Renderer2D.Finish();
		}
		public override void Update(TimeSpan delta)
		{
			if (Game.InitialLoadComplete)
			{
				Game.ResourceManager.Preload();
				Game.Fonts.LoadFonts();
				if (Game.Config.CustomState != null)
					Game.ChangeState(Game.Config.CustomState(Game));
				else
					Game.ChangeState(new MainMenu(Game));
			}
		}
	}
}

