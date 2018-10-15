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
 * Portions created by the Initial Developer are Copyright (C) 2013-2018
 * the Initial Developer. All Rights Reserved.
 */
using System;
using LibreLancer.Media;
namespace LibreLancer
{
	public class IntroMovie : GameState
	{
		VideoPlayer player;
		int idx = int.MaxValue;
        bool inited = false;
		public IntroMovie(FreelancerGame game, int index) : base(game)
		{
			player = new VideoPlayer(game, game.MpvOverride);
			if ((inited = player.Init()) && game.IntroMovies.Count > 0)
            {
                idx = index;
                game.Keyboard.KeyDown += HandleKeyDown;
                player.PlayFile(game.IntroMovies[index]);
            }
            else
            {
                Leave();
            }
		}

		public override void Draw(TimeSpan delta)
		{
			if (idx != int.MaxValue)
			{
				player.Draw(Game.RenderState);
				if (!player.Playing)
				{
					Leave();
					return;
				}
				var tex = player.GetTexture();
				Game.Renderer2D.Start(Game.Width, Game.Height);
				Game.Renderer2D.DrawImageStretched(tex, new Rectangle(0, 0, Game.Width, Game.Height), Color4.White);
				Game.Renderer2D.Finish();
			}
			else
				Leave();
		}

		void HandleKeyDown(KeyEventArgs args)
		{
			Leave();
		}

		void Leave()
		{
			Game.Keyboard.KeyDown -= HandleKeyDown;
			player.Dispose();
			if ((idx + 1) >= Game.IntroMovies.Count || !inited || Game.IntroMovies.Count <= 0)
			{
				Game.ChangeState(new LoadingDataState(Game));
			}
			else
				Game.ChangeState(new IntroMovie(Game, idx + 1));
		}

		public override void Update(TimeSpan delta)
		{
			
		}
	}
}

